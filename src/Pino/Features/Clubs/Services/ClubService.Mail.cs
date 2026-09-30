using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Features.Clubs.Models;
using Pino.SharedKernel.Clubs;

namespace Pino.Features.Clubs.Services;

internal sealed partial class ClubService
{
    internal async Task<StaffEmailsPage> GetEmailsAsync(ClaimsPrincipal actor, Guid clubId, int page, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdministratorAsync(db, await VerifiedActorAsync(db, actor, cancellationToken), clubId, cancellationToken);
        page = Math.Clamp(page, 0, 10000);
        var items = await db.StaffEmails.AsNoTracking().Where(value => value.ClubId == clubId)
            .OrderByDescending(value => value.CreatedAt).ThenBy(value => value.Id).Skip(page * PageSize).Take(PageSize + 1)
            .Select(value => new StaffEmailSummary(value.Id, value.RecipientEmail, value.Subject, value.Status, value.CreatedAt, value.SentAt, value.Attempts, value.Revision))
            .ToArrayAsync(cancellationToken);
        return new(Summary(await db.Clubs.SingleAsync(value => value.Id == clubId, cancellationToken)), items.Take(PageSize).ToArray(), page, items.Length > PageSize);
    }

    internal Task<ClubOperationOutcome> RetryEmailAsync(ClaimsPrincipal actor, Guid clubId, InvitationChangeInput input, CancellationToken cancellationToken) =>
        WriteAsync(actor, async (db, id, ct) =>
        {
            await RequireAdministratorAsync(db, id, clubId, ct);
            var email = await db.StaffEmails.SingleOrDefaultAsync(value => value.ClubId == clubId && value.Id == input.Id, ct);
            if (email is null || email.Revision != input.Revision || email.Status != StaffEmailStatus.Failed)
            {
                return new ClubOperationOutcome.Conflict("This email changed or is no longer failed. Refresh its delivery status.");
            }
            if (email.InvitationId is { } invitationId && !await db.ClubInvitations.AnyAsync(value =>
                value.Id == invitationId && value.ClubId == clubId && value.Revision == email.InvitationRevision &&
                value.UsedAt == null && value.RevokedAt == null && value.ExpiresAt > time.GetUtcNow(), ct))
            {
                email.Status = StaffEmailStatus.Cancelled;
                email.ProtectedBody = "";
                email.Revision++;
                return new ClubOperationOutcome.Conflict("The invitation is no longer active. Create or resend an invitation instead.");
            }
            email.Status = StaffEmailStatus.Pending;
            email.Attempts = 0;
            email.NextAttemptAt = time.GetUtcNow();
            email.Revision++;
            return new ClubOperationOutcome.Saved("Email queued for another delivery attempt.");
        }, cancellationToken);
}
