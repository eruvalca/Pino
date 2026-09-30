using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Features.Sporting.Data;
using Pino.Features.Sporting.Models;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal Task<SportOutcome> SaveTeamTargetsAsync(ClaimsPrincipal actor, Guid clubId, Guid teamId, TeamTargetsInput input, CancellationToken ct) =>
        WriteAsync<SportOutcome>(actor, clubId, async (db, actorId, token) =>
        {
            await RequireAdministratorAsync(db, actorId, clubId, token);
            if (!ValidTeamTargets(input)) { return new SportOutcome.Invalid("Use optional targets from 0 to 2,000 and distinct position names of up to 80 characters. A team can have up to 100 position targets."); }
            var team = await db.SportTeams.Include(value => value.PositionTargets).SingleOrDefaultAsync(value => value.ClubId == clubId && value.Id == teamId, token);
            if (team is not { Archived: false }) { return new SportOutcome.Invalid("Restore this club team before changing its targets."); }
            if (team.Revision != input.Revision) { return new SportOutcome.Conflict(StaleMessage); }
            var targets = input.Positions.ToDictionary(value => value.Position.Trim().ToUpperInvariant(), StringComparer.Ordinal);
            foreach (var existing in team.PositionTargets.ToArray())
            {
                if (!targets.ContainsKey(existing.PositionKey)) { db.TeamPositionTargets.Remove(existing); }
            }
            foreach (var (key, target) in targets)
            {
                var row = team.PositionTargets.SingleOrDefault(value => string.Equals(value.PositionKey, key, StringComparison.Ordinal));
                if (row is null)
                {
                    row = new TeamPositionTarget { ClubId = clubId, TeamId = teamId, PositionKey = key };
                    db.TeamPositionTargets.Add(row);
                }
                row.Position = target.Position.Trim();
                row.Players = target.Players;
            }
            team.RosterTarget = input.RosterTarget;
            team.Revision++;
            return new SportOutcome.Saved("Roster targets saved. Targets never stop a valid placement.", teamId);
        }, ct);

    private static bool ValidTeamTargets(TeamTargetsInput input) => (input.RosterTarget is null or >= 0 and <= 2000) &&
        input.Positions is { Count: <= 100 } && input.Positions.All(value => value is not null && !string.IsNullOrWhiteSpace(value.Position) && value.Position.Length <= 80 && value.Players is >= 0 and <= 2000) &&
        input.Positions.Select(value => value.Position.Trim().ToUpperInvariant()).Distinct(StringComparer.Ordinal).Count() == input.Positions.Count;
}
