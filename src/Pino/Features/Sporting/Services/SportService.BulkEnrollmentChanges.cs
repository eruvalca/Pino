using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Features.Sporting.Data;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal Task<SportingBatchReport> ChangeEnrollmentsAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, BulkEnrollmentChangeInput input, CancellationToken ct) =>
        WriteAsync(actor, clubId, async (db, actorId, token) =>
        {
            var action = input.Remove ? "ExcludePlayers" : "RestorePlayers";
            var replay = await ReplayedBatchAsync(db, clubId, input.OperationId, actorId, tryoutId, action, token);
            if (replay is not null) { return replay; }
            var reason = input.Reason?.Trim() ?? "";
            if (!ValidEnrollmentBatch(input, reason))
            {
                return InvalidBatch("Select 1 to 1,000 distinct players and give a reason of 1 to 1,000 characters.");
            }
            var tryout = await TryoutAsync(db, clubId, tryoutId, token);
            if (tryout is not { Closed: false } || await SeasonAsync(db, clubId, tryout.SeasonId, token) is not { Archived: false }) { return InvalidBatch("Reopen the season and tryout before changing its players."); }
            var ids = input.Players.Select(value => value.PlayerId).ToArray();
            var entries = await db.Participations.Where(value => value.ClubId == clubId && value.TryoutId == tryoutId).ToDictionaryAsync(value => value.PlayerId, token);
            var players = await db.Players.Where(value => value.ClubId == clubId && ids.Contains(value.Id)).ToDictionaryAsync(value => value.Id, token);
            var placements = await db.SeasonPlacements.Where(value => value.ClubId == clubId && value.SeasonId == tryout.SeasonId && ids.Contains(value.PlayerId)).ToDictionaryAsync(value => value.PlayerId, token);
            var retained = entries.Values.Count(value => !value.Removed);
            var bibs = entries.Values.Where(value => !value.Removed && value.Bib.Length > 0).Select(value => value.Bib).ToHashSet(StringComparer.Ordinal);
            var author = await AuthorAsync(db, actorId, token);
            var rows = new List<SportingBatchRow>();
            foreach (var selected in input.Players)
            {
                var entry = entries.GetValueOrDefault(selected.PlayerId);
                var placement = placements.GetValueOrDefault(selected.PlayerId);
                if (entry is null || placement is null || entry.Revision != selected.Revision || placement.Revision != selected.PlacementRevision || entry.Removed == input.Remove)
                {
                    rows.Add(new(selected.PlayerId, "This player's tryout entry or team changed. Refresh and review them again.", Applied: false));
                    continue;
                }
                if (!input.Remove && !CanRestoreEnrollment(entry, players.GetValueOrDefault(selected.PlayerId), retained, bibs))
                {
                    rows.Add(new(selected.PlayerId, "Could not restore this player. Open their entry to check whether they are archived, the tryout is full or their bib is in use.", Applied: false));
                    continue;
                }
                ApplyEnrollmentChange(db, actorId, author, tryout, entry, placement,
                    new(Guid.NewGuid(), selected.PlayerId, input.Remove, reason, selected.Revision, selected.PlacementRevision, entry.Bib), reason, entry.Bib);
                retained += input.Remove ? -1 : 1;
                if (input.Remove) { bibs.Remove(entry.Bib); }
                else if (entry.Bib.Length > 0) { bibs.Add(entry.Bib); }
                rows.Add(new(selected.PlayerId, input.Remove ? "Excluded; history retained." : "Restored; a new final decision is needed.", Applied: true));
            }
            return CompleteBatch(db, clubId, input.OperationId, actorId, tryoutId, action, rows);
        }, ct);

    private static bool ValidEnrollmentBatch(BulkEnrollmentChangeInput input, string reason) =>
        reason.Length is >= 1 and <= 1000 && input.Players is { Count: > 0 and <= 1000 } &&
        input.Players.All(value => value is not null) &&
        input.Players.Select(value => value.PlayerId).Distinct().Count() == input.Players.Count;

    private static bool CanRestoreEnrollment(Participation entry, Player? player, int retained, HashSet<string> bibs) =>
        player is { Archived: false } && retained < 2000 && (entry.Bib.Length == 0 || !bibs.Contains(entry.Bib));
}
