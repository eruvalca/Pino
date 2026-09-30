using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Data;
using Pino.Features.Sporting.Data;
using Pino.Features.Sporting.Models;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    private static IQueryable<SportTeam> AvailableTeams(ApplicationDbContext db, Guid clubId, Guid seasonId, Guid? tryoutId = null) =>
        db.SportTeams.Where(team => team.ClubId == clubId && !team.Archived &&
            !db.SeasonTeamAvailabilities.Any(rule => rule.ClubId == clubId && rule.SeasonId == seasonId && rule.TeamId == team.Id && rule.Excluded) &&
            !db.TryoutTeamAvailabilities.Any(rule => rule.ClubId == clubId && rule.TryoutId == tryoutId && rule.TeamId == team.Id && rule.Excluded));

    private static async Task<TeamSummary[]> ReadTeamsAsync(ApplicationDbContext db, Guid clubId, Guid seasonId, Guid? tryoutId, CancellationToken ct)
    {
        var teams = await db.SportTeams.AsNoTracking().Include(value => value.PositionTargets).Where(value => value.ClubId == clubId).OrderBy(value => value.Name).ToListAsync(ct);
        var excluded = (await db.SeasonTeamAvailabilities.Where(value => value.ClubId == clubId && value.SeasonId == seasonId && value.Excluded).Select(value => value.TeamId)
            .Union(db.TryoutTeamAvailabilities.Where(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.Excluded).Select(value => value.TeamId)).ToListAsync(ct)).ToHashSet();
        return teams.Select(team => Summary(team) with { Excluded = excluded.Contains(team.Id) }).ToArray();
    }

    internal Task<IReadOnlyList<TeamAvailabilitySummary>> GetTeamAvailabilityAsync(ClaimsPrincipal actor, Guid clubId, Guid seasonId, Guid? tryoutId, CancellationToken ct) =>
        ReadSnapshotAsync<IReadOnlyList<TeamAvailabilitySummary>>(actor, clubId, async (db, token) =>
        {
            if (await SeasonAsync(db, clubId, seasonId, token) is null ||
                (tryoutId is { } id && (await TryoutAsync(db, clubId, id, token))?.SeasonId != seasonId)) { throw new KeyNotFoundException(); }
            var teams = await db.SportTeams.AsNoTracking().Include(value => value.PositionTargets).Where(value => value.ClubId == clubId).OrderBy(value => value.Name).ToListAsync(token);
            var seasonRules = await db.SeasonTeamAvailabilities.AsNoTracking().Where(value => value.ClubId == clubId && value.SeasonId == seasonId).ToDictionaryAsync(value => value.TeamId, token);
            var tryoutRules = await db.TryoutTeamAvailabilities.AsNoTracking().Where(value => value.ClubId == clubId && value.TryoutId == tryoutId).ToDictionaryAsync(value => value.TeamId, token);
            return teams.Select(team => new TeamAvailabilitySummary(Summary(team), seasonRules.GetValueOrDefault(team.Id)?.Excluded ?? false,
                seasonRules.GetValueOrDefault(team.Id)?.Revision ?? 0, tryoutRules.GetValueOrDefault(team.Id)?.Excluded ?? false, tryoutRules.GetValueOrDefault(team.Id)?.Revision ?? 0)).ToArray();
        }, ct);

    internal Task<SportOutcome> SaveTeamAvailabilityAsync(ClaimsPrincipal actor, Guid clubId, Guid seasonId, Guid? tryoutId, TeamAvailabilityInput input, CancellationToken ct) =>
        WriteAsync<SportOutcome>(actor, clubId, async (db, actorId, token) =>
        {
            await RequireAdministratorAsync(db, actorId, clubId, token);
            var season = await SeasonAsync(db, clubId, seasonId, token);
            if (season is not { Archived: false }) { return new SportOutcome.Invalid("Reopen this season before changing team availability."); }
            if (!await db.SportTeams.AnyAsync(value => value.ClubId == clubId && value.Id == input.TeamId, token)) { return new SportOutcome.Invalid("Choose a team from this club."); }
            if (tryoutId is { } id)
            {
                var tryout = await TryoutAsync(db, clubId, id, token);
                if (tryout is not { Closed: false } || tryout.SeasonId != seasonId) { return new SportOutcome.Invalid("Choose an open tryout in this season."); }
                var rule = await db.TryoutTeamAvailabilities.SingleOrDefaultAsync(value => value.ClubId == clubId && value.TryoutId == id && value.TeamId == input.TeamId, token);
                if ((rule?.Revision ?? 0) != input.Revision) { return new SportOutcome.Conflict(StaleMessage); }
                if (rule is null)
                {
                    rule = new() { ClubId = clubId, TryoutId = id, TeamId = input.TeamId };
                    db.TryoutTeamAvailabilities.Add(rule);
                }
                rule.Excluded = input.Excluded;
                rule.Revision++;
                tryout.Revision++;
            }
            else
            {
                var rule = await db.SeasonTeamAvailabilities.SingleOrDefaultAsync(value => value.ClubId == clubId && value.SeasonId == seasonId && value.TeamId == input.TeamId, token);
                if ((rule?.Revision ?? 0) != input.Revision) { return new SportOutcome.Conflict(StaleMessage); }
                if (rule is null)
                {
                    rule = new() { ClubId = clubId, SeasonId = seasonId, TeamId = input.TeamId };
                    db.SeasonTeamAvailabilities.Add(rule);
                }
                rule.Excluded = input.Excluded;
                rule.Revision++;
                season.Revision++;
            }
            return new SportOutcome.Saved(input.Excluded ? "Team excluded from new placements. Saved rosters and decisions are unchanged." : "Team included here. If the team is excluded from the season, it still cannot receive new players.", input.TeamId);
        }, ct);
}
