using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Features.Sporting.Data;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal async Task<TeamDetail> GetTeamAsync(ClaimsPrincipal actor, Guid clubId, Guid teamId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireMemberAsync(db, actor, clubId, ct);
        var team = await db.SportTeams.SingleOrDefaultAsync(value => value.ClubId == clubId && value.Id == teamId, ct) ?? throw new KeyNotFoundException();
        var season = await SeasonAsync(db, clubId, team.SeasonId, ct) ?? throw new KeyNotFoundException();
        var members = await (from placement in db.SeasonPlacements
                             join player in db.Players on placement.PlayerId equals player.Id
                             where placement.ClubId == clubId && placement.TeamId == teamId
                             orderby player.LastName, player.FirstName
                             select player).AsNoTracking().ToListAsync(ct);
        var history = await (from decision in db.DecisionEvents
                             join player in db.Players on decision.PlayerId equals player.Id
                             where decision.ClubId == clubId && (decision.TeamId == teamId || decision.PreviousTeamId == teamId)
                             orderby decision.CreatedAt descending, decision.Id descending
                             select new { Player = player, Decision = decision }).AsNoTracking().ToListAsync(ct);
        return new(Summary(team), Summary(season), members.Select(Summary).ToArray(), history.Select(value => new TeamHistoryEntry(Summary(value.Player), Summary(value.Decision))).ToArray());
    }
    private static PlayerSummary Summary(Player player) => new(player.Id, player.PlayerReference, player.FirstName, player.LastName,
        player.GraduationYear, player.Position, player.ContactEmail, player.Archived, player.Revision,
        player.PhotoKey is null ? null : new Uri($"/api/clubs/{player.ClubId}/sport/players/{player.Id}/photo?v={player.PhotoKey}", UriKind.Relative));
    private static SeasonSummary Summary(Season season) => new(season.Id, season.Name, season.StartsOn, season.EndsOn, season.Archived, season.Revision);
    private static TeamSummary Summary(SportTeam team) => new(team.Id, team.SeasonId, team.Name, team.GraduationYear, team.Archived, team.Revision);
    private static DecisionSummary Summary(DecisionEvent decision) => new(decision.Id, decision.PlayerId, decision.TryoutId, decision.TryoutName,
        decision.SeasonName, decision.Kind, decision.TeamName, decision.Author, decision.CreatedAt, decision.Reason, decision.TeamId);

    internal async Task<SportOverview> GetOverviewAsync(ClaimsPrincipal actor, Guid clubId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireMemberAsync(db, actor, clubId, ct);
        var seasons = await db.Seasons.AsNoTracking().Where(value => value.ClubId == clubId).OrderByDescending(value => value.StartsOn).ToListAsync(ct);
        var teams = await db.SportTeams.AsNoTracking().Where(value => value.ClubId == clubId).OrderBy(value => value.Name).ToListAsync(ct);
        var tryouts = await db.TryoutEvents.AsNoTracking().Where(value => value.ClubId == clubId).OrderByDescending(value => value.Date)
            .Select(value => new TryoutSummary(value.Id, value.SeasonId, value.Name, value.Date, value.Location,
                db.Participations.Count(entry => entry.ClubId == clubId && entry.TryoutId == value.Id),
                db.Participations.Count(entry => entry.ClubId == clubId && entry.TryoutId == value.Id && entry.Decision != DecisionKind.Awaiting), value.Revision)).ToListAsync(ct);
        return new(seasons.Select(Summary).ToArray(), teams.Select(Summary).ToArray(), tryouts);
    }

    internal async Task<PlayerPage> GetPlayersAsync(ClaimsPrincipal actor, Guid clubId, string query, bool archived, int page, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireMemberAsync(db, actor, clubId, ct);
        page = Math.Clamp(page, 0, 100000);
        var term = (query ?? "").Trim();
        if (term.Length > 120) { return new([], page, HasMore: false); }
        var players = await db.Players.AsNoTracking().Where(value => value.ClubId == clubId && value.Archived == archived &&
                EF.Functions.ILike(value.FirstName + " " + value.LastName + " " + value.PlayerReference, "%" + SportRules.EscapeLike(term) + "%", "\\"))
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
        return new(Summary(player), placements, history.Select(Summary).ToArray());
    }

    internal async Task<TryoutDetail> GetTryoutAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var actorId = await RequireMemberAsync(db, actor, clubId, ct);
        var tryout = await TryoutAsync(db, clubId, tryoutId, ct) ?? throw new KeyNotFoundException();
        var season = await SeasonAsync(db, clubId, tryout.SeasonId, ct) ?? throw new KeyNotFoundException();
        var teams = await db.SportTeams.AsNoTracking().Where(value => value.ClubId == clubId && value.SeasonId == season.Id).OrderBy(value => value.Name).ToListAsync(ct);
        var entries = await (from entry in db.Participations
                             join player in db.Players on entry.PlayerId equals player.Id
                             join placement in db.SeasonPlacements on new { entry.ClubId, SeasonId = season.Id, entry.PlayerId } equals new { placement.ClubId, placement.SeasonId, placement.PlayerId }
                             where entry.ClubId == clubId && entry.TryoutId == tryoutId
                             orderby player.LastName, player.FirstName, player.Id
                             select new { Entry = entry, Player = player, Placement = placement }).AsNoTracking().ToListAsync(ct);
        var roster = entries.Select(value => new RosterEntry(Summary(value.Player), value.Entry.Bib, value.Entry.Decision, value.Entry.TeamId,
            value.Entry.Revision, value.Placement.TeamId, value.Placement.TryoutId, value.Placement.Revision)).ToArray();
        var notes = await db.PlayerNotes.AsNoTracking().Where(value => value.ClubId == clubId && value.TryoutId == tryoutId).OrderByDescending(value => value.CreatedAt).ThenByDescending(value => value.Id).ToListAsync(ct);
        var corrected = notes.Where(note => note.CorrectsId.HasValue).Select(note => note.CorrectsId!.Value).ToHashSet();
        var history = await db.DecisionEvents.AsNoTracking().Where(value => value.ClubId == clubId && value.TryoutId == tryoutId).OrderByDescending(value => value.CreatedAt).ThenByDescending(value => value.Id).ToListAsync(ct);
        return new(new(tryout.Id, season.Id, tryout.Name, tryout.Date, tryout.Location, roster.Length, roster.Count(entry => entry.Decision != DecisionKind.Awaiting), tryout.Revision),
            Summary(season), teams.Select(Summary).ToArray(), roster,
            notes.Select(note => new NoteSummary(note.Id, note.PlayerId, note.Text, note.Author, note.CreatedAt, note.CorrectsId,
                string.Equals(note.AuthorId, actorId, StringComparison.Ordinal) && !corrected.Contains(note.Id))).ToArray(), history.Select(Summary).ToArray());
    }

    internal async Task<string?> GetPhotoKeyAsync(ClaimsPrincipal actor, Guid clubId, Guid playerId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireMemberAsync(db, actor, clubId, ct);
        return await db.Players.Where(value => value.ClubId == clubId && value.Id == playerId).Select(value => value.PhotoKey).SingleOrDefaultAsync(ct);
    }
}
