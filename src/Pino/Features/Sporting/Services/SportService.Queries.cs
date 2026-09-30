using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Features.Sporting.Data;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal async Task<TeamDetail> GetTeamAsync(ClaimsPrincipal actor, Guid clubId, Guid teamId, Guid? seasonId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireMemberAsync(db, actor, clubId, ct);
        var team = await db.SportTeams.Include(value => value.PositionTargets).SingleOrDefaultAsync(value => value.ClubId == clubId && value.Id == teamId, ct) ?? throw new KeyNotFoundException();
        var seasons = await db.Seasons.AsNoTracking().Where(value => value.ClubId == clubId).OrderBy(value => value.Archived).ThenByDescending(value => value.StartsOn).ThenBy(value => value.Id).ToListAsync(ct);
        var season = seasonId is { } selected ? seasons.SingleOrDefault(value => value.Id == selected) ?? throw new KeyNotFoundException() : seasons.FirstOrDefault();
        var selectedSeasonId = season?.Id;
        var members = await (from placement in db.SeasonPlacements
                             join player in db.Players on placement.PlayerId equals player.Id
                             where placement.ClubId == clubId && placement.TeamId == teamId && placement.SeasonId == selectedSeasonId
                             orderby player.LastName, player.FirstName
                             select player).AsNoTracking().ToListAsync(ct);
        var history = await (from decision in db.DecisionEvents
                             join player in db.Players on decision.PlayerId equals player.Id
                             where decision.ClubId == clubId && (decision.TeamId == teamId || decision.PreviousTeamId == teamId)
                             orderby decision.CreatedAt descending, decision.Id descending
                             select new { Player = player, Decision = decision }).AsNoTracking().ToListAsync(ct);
        return new(Summary(team), season is null ? null : Summary(season), members.Select(Summary).ToArray(), history.Select(value => new TeamHistoryEntry(Summary(value.Player), Summary(value.Decision))).ToArray(), seasons.Select(Summary).ToArray());
    }
    private static PlayerSummary Summary(Player player) => new(player.Id, player.PlayerReference, player.FirstName, player.LastName,
        player.GraduationYear, player.Position, player.ContactEmail, player.Archived, player.Revision,
        player.PhotoKey is null ? null : new Uri($"/api/clubs/{player.ClubId}/sport/players/{player.Id}/photo?v={player.PhotoKey}", UriKind.Relative), player.MiddleName, player.SecondaryPosition);
    private static SeasonSummary Summary(Season season) => new(season.Id, season.Name, season.StartsOn, season.EndsOn, season.Archived, season.Revision);
    private static TeamSummary Summary(SportTeam team) => new(team.Id, team.Name, team.GraduationYear, team.Archived, team.Revision, team.RosterTarget,
        team.PositionTargets.OrderBy(value => value.Position, StringComparer.OrdinalIgnoreCase).Select(value => new PositionTarget(value.Position, value.Players)).ToArray());
    private static DecisionSummary Summary(DecisionEvent decision) => new(decision.Id, decision.PlayerId, decision.TryoutId, decision.TryoutName,
        decision.SeasonName, decision.Kind, decision.TeamName, decision.Author, decision.CreatedAt, decision.Reason, decision.TeamId, StaffPhoto(decision.AuthorId));

    private static Uri? StaffPhoto(string? userId) => string.IsNullOrEmpty(userId) ? null : new($"/api/clubs/photos/{Uri.EscapeDataString(userId)}", UriKind.Relative);

    internal async Task<SportOverview> GetOverviewAsync(ClaimsPrincipal actor, Guid clubId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireMemberAsync(db, actor, clubId, ct);
        var seasons = await db.Seasons.AsNoTracking().Where(value => value.ClubId == clubId).OrderByDescending(value => value.StartsOn).ToListAsync(ct);
        var teams = await db.SportTeams.AsNoTracking().Include(value => value.PositionTargets).Where(value => value.ClubId == clubId).OrderBy(value => value.Name).ToListAsync(ct);
        var tryouts = await db.TryoutEvents.AsNoTracking().Where(value => value.ClubId == clubId).OrderByDescending(value => value.Date)
            .Select(value => new TryoutSummary(value.Id, value.SeasonId, value.Name, value.Date, value.Location,
                db.Participations.Count(entry => entry.ClubId == clubId && entry.TryoutId == value.Id && !entry.Removed),
                db.Participations.Count(entry => entry.ClubId == clubId && entry.TryoutId == value.Id && !entry.Removed && entry.Decision != DecisionKind.Awaiting), value.Revision, value.Closed)).ToListAsync(ct);
        var activePlayers = await db.Players.CountAsync(value => value.ClubId == clubId && !value.Archived, ct);
        return new(seasons.Select(Summary).ToArray(), teams.Select(Summary).ToArray(), tryouts, activePlayers);
    }

    internal async Task<PlayerPage> GetPlayersAsync(ClaimsPrincipal actor, Guid clubId, string query, bool archived, int page, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireMemberAsync(db, actor, clubId, ct);
        page = Math.Clamp(page, 0, 100000);
        var term = (query ?? "").Trim();
        if (term.Length > 120) { return new([], page, HasMore: false); }
        var players = await db.Players.AsNoTracking().Where(value => value.ClubId == clubId && value.Archived == archived &&
                EF.Functions.ILike(value.FirstName + " " + value.LastName + " " + value.FirstName + " " + value.MiddleName + " " + value.LastName + " " + value.PlayerReference, "%" + SportRules.EscapeLike(term) + "%", "\\"))
            .OrderBy(value => value.LastName).ThenBy(value => value.FirstName).ThenBy(value => value.Id).Skip(page * 50).Take(51).ToListAsync(ct);
        return new(players.Take(50).Select(Summary).ToArray(), page, players.Count > 50);
    }

    internal async Task<PlayerDetail> GetPlayerAsync(ClaimsPrincipal actor, Guid clubId, Guid playerId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireMemberAsync(db, actor, clubId, ct);
        var player = await db.Players.AsNoTracking().SingleOrDefaultAsync(value => value.ClubId == clubId && value.Id == playerId, ct) ?? throw new KeyNotFoundException();
        var placements = await (from placement in db.SeasonPlacements
                                join season in db.Seasons on placement.SeasonId equals season.Id
                                join team in db.SportTeams on placement.TeamId equals team.Id into teams
                                from team in teams.DefaultIfEmpty()
                                join tryout in db.TryoutEvents on placement.TryoutId equals tryout.Id into tryouts
                                from tryout in tryouts.DefaultIfEmpty()
                                where placement.ClubId == clubId && placement.PlayerId == playerId
                                orderby season.StartsOn descending
                                select new PlacementSummary(season.Id, season.Name, placement.TeamId, team == null ? null : team.Name, tryout == null ? null : tryout.Name)).ToListAsync(ct);
        var history = await db.DecisionEvents.AsNoTracking().Where(value => value.ClubId == clubId && value.PlayerId == playerId).OrderByDescending(value => value.CreatedAt).ThenByDescending(value => value.Id).ToListAsync(ct);
        var participated = await (from entry in db.Participations
                                  join tryout in db.TryoutEvents on entry.TryoutId equals tryout.Id
                                  join season in db.Seasons on tryout.SeasonId equals season.Id
                                  join team in db.SportTeams on entry.TeamId equals team.Id into teams
                                  from team in teams.DefaultIfEmpty()
                                  where entry.ClubId == clubId && entry.PlayerId == playerId
                                  orderby tryout.Date descending, tryout.Name, tryout.Id
                                  select new PlayerTryoutSummary(tryout.Id, tryout.Name, season.Name, tryout.Date, entry.Bib, entry.Removed, tryout.Closed, entry.Decision, team == null ? null : team.Name)).ToListAsync(ct);
        return new(Summary(player), placements, history.Select(Summary).ToArray(), participated);
    }

    internal async Task<TryoutDetail> GetTryoutAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireMemberAsync(db, actor, clubId, ct);
        var tryout = await TryoutAsync(db, clubId, tryoutId, ct) ?? throw new KeyNotFoundException();
        var season = await SeasonAsync(db, clubId, tryout.SeasonId, ct) ?? throw new KeyNotFoundException();
        var teams = await ReadTeamsAsync(db, clubId, season.Id, tryoutId, ct);
        var entries = await (from entry in db.Participations
                             join player in db.Players on entry.PlayerId equals player.Id
                             join placement in db.SeasonPlacements on new { entry.ClubId, SeasonId = season.Id, entry.PlayerId } equals new { placement.ClubId, placement.SeasonId, placement.PlayerId }
                             where entry.ClubId == clubId && entry.TryoutId == tryoutId && !entry.Removed
                             orderby player.LastName, player.FirstName, player.Id
                             select new { Entry = entry, Player = player, Placement = placement }).AsNoTracking().ToListAsync(ct);
        var observedPlayers = (await db.PlayerNotes.Where(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.RedactedAt == null)
            .Select(value => value.PlayerId).Distinct().ToListAsync(ct)).ToHashSet();
        var playerIds = entries.Select(value => value.Player.Id).ToArray();
        var previous = await (from placement in db.SeasonPlacements
                              join prior in db.Seasons on placement.SeasonId equals prior.Id
                              join team in db.SportTeams on placement.TeamId equals team.Id
                              where placement.ClubId == clubId && prior.StartsOn < season.StartsOn && playerIds.Contains(placement.PlayerId)
                              orderby prior.StartsOn descending, prior.EndsOn descending, prior.Id
                              select new { placement.PlayerId, Placement = new PreviousPlacementSummary(prior.Id, prior.Name, team.Id, team.Name) }).AsNoTracking().ToListAsync(ct);
        var previousByPlayer = previous.ToLookup(value => value.PlayerId);
        var roster = entries.Select(value => new RosterEntry(Summary(value.Player), value.Entry.Bib, value.Entry.Decision, value.Entry.TeamId,
            value.Entry.Revision, value.Placement.TeamId, value.Placement.TryoutId, value.Placement.Revision, observedPlayers.Contains(value.Player.Id), previousByPlayer[value.Player.Id].FirstOrDefault()?.Placement)).ToArray();
        var removed = await db.Participations.CountAsync(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.Removed, ct);
        return new(new(tryout.Id, season.Id, tryout.Name, tryout.Date, tryout.Location, roster.Length, roster.Count(entry => entry.Decision != DecisionKind.Awaiting), tryout.Revision, tryout.Closed),
            Summary(season), teams, roster, removed);
    }

    internal async Task<string?> GetPhotoKeyAsync(ClaimsPrincipal actor, Guid clubId, Guid playerId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireMemberAsync(db, actor, clubId, ct);
        return await db.Players.Where(value => value.ClubId == clubId && value.Id == playerId).Select(value => value.PhotoKey).SingleOrDefaultAsync(ct);
    }
}
