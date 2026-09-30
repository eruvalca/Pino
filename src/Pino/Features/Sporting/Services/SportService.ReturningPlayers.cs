using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Data;
using Pino.Features.Sporting.Data;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal Task<IReadOnlyList<ReturningPlayerReview>> GetReturningPlayersAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, Guid sourceSeasonId, CancellationToken ct) =>
        ReadSnapshotAsync(actor, clubId, async (db, token) =>
        {
            await RequireAdministratorAsync(db, actor.FindFirstValue(ClaimTypes.NameIdentifier)!, clubId, token);
            var tryout = await TryoutAsync(db, clubId, tryoutId, token) ?? throw new KeyNotFoundException();
            var target = await SeasonAsync(db, clubId, tryout.SeasonId, token);
            var source = await SeasonAsync(db, clubId, sourceSeasonId, token);
            if (target is null || source is null || source.StartsOn >= target.StartsOn) { throw new KeyNotFoundException(); }
            return await ReturningReviewAsync(db, clubId, tryout, sourceSeasonId, token);
        }, ct);

    internal Task<SportingBatchReport> PlaceReturningPlayersAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, ReturningPlacementInput input, CancellationToken ct) =>
        WriteAsync(actor, clubId, async (db, actorId, token) =>
        {
            await RequireAdministratorAsync(db, actorId, clubId, token);
            var replay = await ReplayedBatchAsync(db, clubId, input.OperationId, actorId, tryoutId, "ReturningPlacement", token);
            if (replay is not null) { return replay; }
            if (input.Players is not { Count: > 0 and <= 1000 } || input.Players.Any(value => value is null) || input.Players.Select(value => value.PlayerId).Distinct().Count() != input.Players.Count)
            {
                return InvalidBatch("Select between 1 and 1,000 distinct reviewed players.");
            }
            var tryout = await TryoutAsync(db, clubId, tryoutId, token);
            var season = tryout is null ? null : await SeasonAsync(db, clubId, tryout.SeasonId, token);
            var source = await SeasonAsync(db, clubId, input.SourceSeasonId, token);
            if (tryout is not { Closed: false } || season is not { Archived: false } || source is null || source.StartsOn >= season.StartsOn)
            {
                return InvalidBatch("Choose an earlier source season and an open tryout in an active target season.");
            }
            var current = (await ReturningReviewAsync(db, clubId, tryout, input.SourceSeasonId, token)).ToDictionary(value => value.Player.Id);
            var ids = input.Players.Select(value => value.PlayerId).ToArray();
            var entries = await db.Participations.Where(value => value.ClubId == clubId && value.TryoutId == tryoutId && ids.Contains(value.PlayerId)).ToDictionaryAsync(value => value.PlayerId, token);
            var placements = await db.SeasonPlacements.Where(value => value.ClubId == clubId && value.SeasonId == season.Id && ids.Contains(value.PlayerId)).ToDictionaryAsync(value => value.PlayerId, token);
            var teams = await AvailableTeams(db, clubId, season.Id, tryout.Id).ToDictionaryAsync(value => value.Id, token);
            var author = await AuthorAsync(db, actorId, token);
            var rows = new List<SportingBatchRow>();
            foreach (var selection in input.Players)
            {
                var reviewed = current.GetValueOrDefault(selection.PlayerId);
                if (reviewed?.Selection != selection)
                {
                    rows.Add(new(selection.PlayerId, reviewed?.Selection is null ? reviewed?.Message ?? "Source placement or player is no longer available." : "Record changed after review. Refresh and review again.", Applied: false));
                    continue;
                }
                var decision = new DecisionInput(Guid.NewGuid(), selection.PlayerId, DecisionKind.Placed, selection.TargetTeamId, selection.EntryRevision, selection.PlacementRevision, $"Returning placement reviewed from {reviewed.SourceTeam}.");
                ApplyDecision(db, actorId, author, decision, tryout, season, entries[selection.PlayerId], placements[selection.PlayerId], teams[selection.TargetTeamId]);
                rows.Add(new(selection.PlayerId, $"Placed on {reviewed.TargetTeam}. Previous seasons are unchanged.", Applied: true));
            }
            return CompleteBatch(db, clubId, input.OperationId, actorId, tryoutId, "ReturningPlacement", rows);
        }, ct);

    private static async Task<IReadOnlyList<ReturningPlayerReview>> ReturningReviewAsync(ApplicationDbContext db, Guid clubId, TryoutEvent tryout, Guid sourceSeasonId, CancellationToken ct)
    {
        var sources = await (from placement in db.SeasonPlacements
                             join player in db.Players on placement.PlayerId equals player.Id
                             join team in db.SportTeams on placement.TeamId equals team.Id
                             where placement.ClubId == clubId && placement.SeasonId == sourceSeasonId
                             orderby player.LastName, player.FirstName, player.Id
                             select new { Player = player, Placement = placement, Team = team }).AsNoTracking().ToListAsync(ct);
        var teams = await AvailableTeams(db, clubId, tryout.SeasonId, tryout.Id).AsNoTracking().ToDictionaryAsync(value => value.Id, ct);
        var entries = await db.Participations.AsNoTracking().Where(value => value.ClubId == clubId && value.TryoutId == tryout.Id).ToDictionaryAsync(value => value.PlayerId, ct);
        var placements = await db.SeasonPlacements.AsNoTracking().Where(value => value.ClubId == clubId && value.SeasonId == tryout.SeasonId).ToDictionaryAsync(value => value.PlayerId, ct);
        return sources.Select(source => ReviewReturningPlayer(source.Player, source.Placement, source.Team,
            teams, entries.GetValueOrDefault(source.Player.Id), placements.GetValueOrDefault(source.Player.Id))).ToArray();
    }

    private static ReturningPlayerReview ReviewReturningPlayer(Player player, SeasonPlacement source, SportTeam sourceTeam,
        IReadOnlyDictionary<Guid, SportTeam> teams, Participation? entry, SeasonPlacement? placement)
    {
        var target = teams.GetValueOrDefault(sourceTeam.Id);
        var problem = (player, target, entry, placement) switch
        {
            ({ Archived: true }, _, _, _) => "Player is archived. Open their record to restore them first.",
            (_, not { Archived: false }, _, _) => "The previous team is archived or excluded from this season or tryout.",
            (_, { } team, _, _) when !SportRules.Eligible(player.GraduationYear, team.GraduationYear) => "Graduation year is not eligible for the previous team.",
            (_, _, { Removed: true }, _) => "This player was excluded. Open Manage tryout players to restore them.",
            (_, _, null, _) or (_, _, _, null) => "Enroll this player in the tryout first.",
            (_, _, { Decision: not DecisionKind.Awaiting }, _) => "An outcome is already recorded. Review it in the notebook.",
            (_, _, _, { TeamId: not null }) => "A current season placement already exists. Review it in the notebook.",
            _ => null,
        };
        ReturningSelection? selection = problem is null ? new(player.Id, sourceTeam.Id, target!.Id, player.Revision, source.Revision, entry!.Revision, placement!.Revision, target.Revision) : null;
        return new(Summary(player), sourceTeam.Name, target?.Name, selection, problem ?? "Ready for reviewed placement.");
    }
}
