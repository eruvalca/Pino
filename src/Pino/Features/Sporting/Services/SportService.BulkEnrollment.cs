using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal Task<IReadOnlyList<EnrollmentCandidate>> GetEnrollmentCandidatesAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, CancellationToken ct) =>
        ReadSnapshotAsync<IReadOnlyList<EnrollmentCandidate>>(actor, clubId, async (db, token) =>
        {
            await RequireAdministratorAsync(db, actor.FindFirstValue(ClaimTypes.NameIdentifier)!, clubId, token);
            if (await TryoutAsync(db, clubId, tryoutId, token) is null) { throw new KeyNotFoundException(); }
            var players = await db.Players.AsNoTracking().Where(value => value.ClubId == clubId && !value.Archived).OrderBy(value => value.LastName).ThenBy(value => value.FirstName).ThenBy(value => value.Id).ToListAsync(token);
            var entries = await db.Participations.AsNoTracking().Where(value => value.ClubId == clubId && value.TryoutId == tryoutId).ToDictionaryAsync(value => value.PlayerId, token);
            return players.Select(player => new EnrollmentCandidate(Summary(player), entries.ContainsKey(player.Id), entries.GetValueOrDefault(player.Id)?.Removed == true)).ToArray();
        }, ct);

    internal Task<SportingBatchReport> EnrollBulkAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, BulkEnrollmentInput input, CancellationToken ct) =>
        WriteAsync(actor, clubId, async (db, actorId, token) =>
        {
            await RequireAdministratorAsync(db, actorId, clubId, token);
            var replay = await ReplayedBatchAsync(db, clubId, input.OperationId, actorId, tryoutId, "Enrollment", token);
            if (replay is not null) { return replay; }
            if (input.Players is not { Count: > 0 and <= 1000 } || input.Players.Any(value => value is null) || input.Players.Select(value => value.PlayerId).Distinct().Count() != input.Players.Count)
            {
                return InvalidBatch("Select between 1 and 1,000 distinct players for this batch.");
            }
            var tryout = await TryoutAsync(db, clubId, tryoutId, token);
            if (tryout is not { Closed: false } || await SeasonAsync(db, clubId, tryout.SeasonId, token) is not { Archived: false })
            {
                return InvalidBatch("Choose an open tryout in an active season before adding players.");
            }
            var ids = input.Players.Select(value => value.PlayerId).ToArray();
            var players = await db.Players.Where(value => value.ClubId == clubId && ids.Contains(value.Id)).ToDictionaryAsync(value => value.Id, token);
            var entries = await db.Participations.Where(value => value.ClubId == clubId && value.TryoutId == tryoutId).ToDictionaryAsync(value => value.PlayerId, token);
            var placed = await db.SeasonPlacements.Where(value => value.ClubId == clubId && value.SeasonId == tryout.SeasonId && ids.Contains(value.PlayerId)).Select(value => value.PlayerId).ToListAsync(token);
            var placedIds = placed.ToHashSet();
            var capacity = 2000 - entries.Values.Count(value => !value.Removed);
            var rows = new List<SportingBatchRow>();
            foreach (var selection in input.Players)
            {
                var player = players.GetValueOrDefault(selection.PlayerId);
                var entry = entries.GetValueOrDefault(selection.PlayerId);
                var problem = (player, entry, capacity) switch
                {
                    (not { Archived: false }, _, _) => "Player unavailable or archived.",
                    ({ } current, _, _) when current.Revision != selection.PlayerRevision => "Player changed after review. Refresh and review again.",
                    (_, { Removed: true }, _) => "This player was excluded. Open Manage tryout players to restore them.",
                    (_, not null, _) => "Already enrolled.",
                    (_, _, 0) => "Tryout capacity reached. This player was not added.",
                    _ => null,
                };
                if (problem is not null) { rows.Add(new(selection.PlayerId, problem, Applied: false)); continue; }
                db.Participations.Add(new() { ClubId = clubId, TryoutId = tryoutId, PlayerId = selection.PlayerId, Revision = 1 });
                if (!placedIds.Contains(selection.PlayerId)) { db.SeasonPlacements.Add(new() { ClubId = clubId, SeasonId = tryout.SeasonId, PlayerId = selection.PlayerId }); }
                tryout.Revision++;
                capacity--;
                rows.Add(new(selection.PlayerId, "Added with no bib and awaiting an explicit final decision.", Applied: true));
            }
            return CompleteBatch(db, clubId, input.OperationId, actorId, tryoutId, "Enrollment", rows);
        }, ct);
}
