using Microsoft.EntityFrameworkCore;
using Pino.Data;
using Pino.Features.Sporting.Data;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    private static async Task<ImportReport> ReviewImportAsync(ApplicationDbContext db, Guid clubId,
        List<(PlayerInput Player, ImportRow Row)> parsed, IReadOnlyList<ImportResolution>? resolutions, CancellationToken ct)
    {
        var references = parsed.Select(value => value.Player.PlayerReference).ToArray();
        var years = parsed.Select(value => value.Player.GraduationYear).Distinct().ToArray();
        var records = await db.Players.Where(value => value.ClubId == clubId && (years.Contains(value.GraduationYear) || references.Contains(value.PlayerReference))).ToListAsync(ct);
        var rows = parsed.Select(value => ImportMatching.Review(value.Player, value.Row, records, resolutions?.SingleOrDefault(choice => choice.Row == value.Row.Row))).ToArray();
        // A batch must never reactivate or erase the same record twice, or create
        // indistinguishable records merely because its source references differ.
        var duplicateTargets = rows.Where(value => value.PlayerId is not null && value.Disposition is ImportDisposition.Reactivate or ImportDisposition.Replace)
            .GroupBy(value => value.PlayerId).Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet();
        var duplicateNames = rows.Where(value => value.Disposition == ImportDisposition.Create && value.Error is null)
            .GroupBy(value => (Name: value.Name.ToUpperInvariant(), value.GraduationYear)).Where(group => group.Count() > 1).SelectMany(group => group).Select(value => value.Row).ToHashSet();
        rows = rows.Select(value => duplicateTargets.Contains(value.PlayerId) || duplicateNames.Contains(value.Row)
            ? value with { Disposition = ImportDisposition.Error, Error = "This file repeats a player candidate. Skip an extra row or distinguish different people with their middle names before importing." } : value).ToArray();
        var report = new ImportReport(rows, Saved: false, "", Counts: CountImport(rows));
        return report with
        {
            Message = report.CanImport ? "Review the actions below, then complete the import. Existing player fields are never overwritten."
            : "Nothing imported. Resolve or skip the listed rows, then preview again."
        };
    }

    private async Task<ImportReport> ApplyImportAsync(ApplicationDbContext db, string actorId, Guid clubId, Guid operationId,
        List<(PlayerInput Player, ImportRow Row)> parsed, IReadOnlyList<ImportRow> rows, CancellationToken ct)
    {
        var saved = new List<ImportRow>();
        foreach (var row in rows)
        {
            if (row.Disposition == ImportDisposition.Skip) { saved.Add(row); continue; }
            var player = parsed.Single(value => value.Row.Row == row.Row).Player;
            if (row.Disposition == ImportDisposition.Reactivate)
            {
                var existing = await db.Players.SingleAsync(value => value.ClubId == clubId && value.Id == row.PlayerId, ct);
                existing.Archived = false;
                existing.Revision++;
                saved.Add(row with { Disposition = ImportDisposition.Reactivated });
                continue;
            }
            var pending = false;
            if (row.Disposition == ImportDisposition.Replace)
            {
                var existing = await db.Players.SingleAsync(value => value.ClubId == clubId && value.Id == row.PlayerId, ct);
                var receipt = await ErasePlayerRecordsAsync(db, actorId, existing, row.ErasureOperationId!.Value, ct);
                pending = receipt.PhotoKey is not null;
                // Release a reused unique reference within the same transaction.
                await db.SaveChangesAsync(ct);
            }
            var created = new Player { Id = player.Id, ClubId = clubId };
            UpdatePlayerFields(created, player, player.PlayerReference);
            db.Players.Add(created);
            saved.Add(row with
            {
                Disposition = row.Disposition == ImportDisposition.Replace ? ImportDisposition.Replaced : ImportDisposition.Created,
                PlayerId = created.Id,
                PhotoPending = pending,
                Candidates = []
            });
        }
        var counts = CountImport(saved);
        db.PlayerImportReceipts.Add(new()
        {
            Id = operationId,
            ClubId = clubId,
            ActorId = actorId,
            CreatedAt = time.GetUtcNow(),
            Created = counts.Created,
            Skipped = counts.Skipped,
            Reactivated = counts.Reactivated
        });
        return new(saved, Saved: true, $"Created {counts.Created}; skipped {counts.Skipped}; reactivated {counts.Reactivated}." +
            (saved.Any(value => value.PhotoPending) ? " Some erased photos are awaiting automatic deletion; check their completion below." : ""), Counts: counts);
    }

    private static ImportCounts CountImport(IReadOnlyList<ImportRow> rows) => new(
        rows.Count(value => value.Disposition is ImportDisposition.Created or ImportDisposition.Replaced),
        rows.Count(value => value.Disposition == ImportDisposition.Skip),
        rows.Count(value => value.Disposition == ImportDisposition.Reactivated),
        rows.Count(value => value.Disposition == ImportDisposition.Review),
        rows.Count(value => value.Error is not null));
}
