using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Data;
using Pino.Features.Sporting.Data;
using Pino.Features.Sporting.Models;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal Task<ErasureOutcome> ErasePlayerAsync(ClaimsPrincipal actor, Guid clubId, ErasePlayerInput input, CancellationToken ct) =>
        WriteAsync<ErasureOutcome>(actor, clubId, async (db, actorId, token) =>
        {
            await RequireAdministratorAsync(db, actorId, clubId, token);
            if (input.OperationId == Guid.Empty) { return new SportOutcome.Invalid("Review this deletion before confirming it."); }
            var receipt = await db.PlayerErasures.SingleOrDefaultAsync(value => value.Id == input.OperationId, token);
            if (receipt is not null)
            {
                return receipt.ClubId == clubId && string.Equals(receipt.ActorId, actorId, StringComparison.Ordinal)
                    ? ErasureResult(receipt)
                    : new SportOutcome.Conflict(StaleMessage);
            }
            var player = await db.Players.SingleOrDefaultAsync(value => value.ClubId == clubId && value.Id == input.PlayerId, token);
            if (player is null || player.Revision != input.Revision) { return new SportOutcome.Conflict(StaleMessage); }
            if (!ConfirmsErasure(player, input))
            {
                return new SportOutcome.Invalid("Type the player's full name exactly and confirm that their records and history will be permanently deleted.");
            }
            return ErasureResult(await ErasePlayerRecordsAsync(db, actorId, player, input.OperationId, token));
        }, ct);

    internal async Task<ErasureReport> GetErasureAsync(ClaimsPrincipal actor, Guid clubId, Guid operationId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var actorId = await RequireMemberAsync(db, actor, clubId, ct);
        await RequireAdministratorAsync(db, actorId, clubId, ct);
        var receipt = await db.PlayerErasures.SingleOrDefaultAsync(value => value.ClubId == clubId && value.Id == operationId, ct) ?? throw new KeyNotFoundException();
        return ErasureResult(receipt).ToReply();
    }

    private static bool ConfirmsErasure(Player player, ErasePlayerInput input) =>
        input.AcknowledgeHistory && string.Equals(input.Confirmation?.Trim(), Summary(player).FullName, StringComparison.Ordinal);

    private static ErasureOutcome.Recorded ErasureResult(PlayerErasure receipt) => new(receipt.Id, receipt.PhotoKey is not null);

    // Used by standalone erasure and confirmed archived-record replacement. All
    // linked content is removed in the caller's transaction, including closed copies.
    private async Task<PlayerErasure> ErasePlayerRecordsAsync(ApplicationDbContext db, string actorId, Player player, Guid operationId, CancellationToken ct)
    {
        var clubId = player.ClubId;
        var playerId = player.Id;
        await db.TryoutCloseouts.Where(value => value.ClubId == clubId && db.TryoutCloseoutPlayers.Any(copy => copy.CloseoutId == value.Id && copy.PlayerId == playerId))
            .ExecuteUpdateAsync(setters => setters.SetProperty(value => value.ErasedPlayers, value => value.ErasedPlayers + 1), ct);
        await db.TryoutCloseoutPlayers.Where(value => value.PlayerId == playerId && db.TryoutCloseouts.Any(closeout => closeout.Id == value.CloseoutId && closeout.ClubId == clubId)).ExecuteDeleteAsync(ct);
        await db.PlayerNotes.Where(value => value.ClubId == clubId && value.PlayerId == playerId).ExecuteDeleteAsync(ct);
        await db.TryoutAttendances.Where(value => value.ClubId == clubId && value.PlayerId == playerId).ExecuteDeleteAsync(ct);
        await db.EnrollmentChanges.Where(value => value.ClubId == clubId && value.PlayerId == playerId).ExecuteDeleteAsync(ct);
        await db.DecisionEvents.Where(value => value.ClubId == clubId && value.PlayerId == playerId).ExecuteDeleteAsync(ct);
        await db.SeasonPlacements.Where(value => value.ClubId == clubId && value.PlayerId == playerId).ExecuteDeleteAsync(ct);
        var tryoutIds = await db.Participations.Where(value => value.ClubId == clubId && value.PlayerId == playerId).Select(value => value.TryoutId).ToListAsync(ct);
        await db.TryoutEvents.Where(value => value.ClubId == clubId && tryoutIds.Contains(value.Id)).ExecuteUpdateAsync(setters => setters.SetProperty(value => value.Revision, value => value.Revision + 1), ct);
        await db.Participations.Where(value => value.ClubId == clubId && value.PlayerId == playerId).ExecuteDeleteAsync(ct);
        var receipt = new PlayerErasure { Id = operationId, ClubId = clubId, ActorId = actorId, CreatedAt = time.GetUtcNow(), PhotoKey = player.PhotoKey };
        if (player.PhotoKey is { } photoKey && !await db.PhotoDeletions.AnyAsync(value => value.PhotoKey == photoKey, ct))
        {
            db.PhotoDeletions.Add(new() { PhotoKey = photoKey, NotBefore = time.GetUtcNow() });
        }
        db.Players.Remove(player);
        db.PlayerErasures.Add(receipt);
        return receipt;
    }
}
