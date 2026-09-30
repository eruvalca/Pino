using Microsoft.EntityFrameworkCore;
using Pino.Data;
using Pino.Features.Sporting.Data;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    private static async Task<SportingBatchReport?> ReplayedBatchAsync(ApplicationDbContext db, Guid clubId, Guid operationId, string actorId, Guid targetId, string action, CancellationToken ct)
    {
        if (operationId == Guid.Empty) { return InvalidBatch("A batch identifier is required. Reload before saving."); }
        var receipt = await db.SportingBatchReceipts.SingleOrDefaultAsync(value => value.Id == operationId, ct);
        if (receipt is null) { return null; }
        if (receipt.ClubId != clubId || receipt.TargetId != targetId || !string.Equals(receipt.ActorId, actorId, StringComparison.Ordinal) || !string.Equals(receipt.Action, action, StringComparison.Ordinal))
        {
            return new(SportReplyKind.Conflict, "This batch identifier was already used. Reload before starting a new batch.", 0, 0, []);
        }
        return new(SportReplyKind.Saved, $"This batch already completed: {receipt.Applied} applied and {receipt.Skipped} skipped. No new changes were made. Refresh to review current records.", receipt.Applied, receipt.Skipped, [], Replayed: true);
    }

    private SportingBatchReport CompleteBatch(ApplicationDbContext db, Guid clubId, Guid operationId, string actorId, Guid targetId, string action, List<SportingBatchRow> rows)
    {
        var applied = rows.Count(value => value.Applied);
        var skipped = rows.Count - applied;
        RecordBatch(db, clubId, operationId, actorId, targetId, action, applied, skipped);
        return new(SportReplyKind.Saved, $"Batch completed: {applied} applied, {skipped} skipped. Review the per-player results below.", applied, skipped, rows);
    }

    private void RecordBatch(ApplicationDbContext db, Guid clubId, Guid operationId, string actorId, Guid targetId, string action, int applied, int skipped) =>
        db.SportingBatchReceipts.Add(new() { Id = operationId, ClubId = clubId, ActorId = actorId, TargetId = targetId, Action = action, Applied = applied, Skipped = skipped, CreatedAt = time.GetUtcNow() });

    private static SportingBatchReport InvalidBatch(string message) => new(SportReplyKind.Invalid, message, 0, 0, []);
}
