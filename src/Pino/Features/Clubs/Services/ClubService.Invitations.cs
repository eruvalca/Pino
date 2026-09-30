using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Data;
using Pino.Features.Clubs.Data;
using Pino.Features.Clubs.Models;
using Pino.SharedKernel.Clubs;

namespace Pino.Features.Clubs.Services;

internal sealed partial class ClubService
{
    internal async Task<InvitationsPage> GetInvitationsAsync(ClaimsPrincipal actor, Guid clubId, int page, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await RequireAdministratorAsync(db, await VerifiedActorAsync(db, actor, cancellationToken), clubId, cancellationToken);
        page = Math.Clamp(page, 0, 10000);
        var items = await db.ClubInvitations.AsNoTracking().Where(value => value.ClubId == clubId)
            .OrderByDescending(value => value.CreatedAt).ThenBy(value => value.Id).Skip(page * PageSize).Take(PageSize + 1)
            .Select(value => new InvitationSummary(value.Id, value.Email, value.Role, value.ExpiresAt, value.UsedAt, value.RevokedAt, value.Revision,
                db.StaffEmails.Where(email => email.InvitationId == value.Id)
                    .OrderByDescending(email => email.CreatedAt).ThenBy(email => email.Id).Select(email => (StaffEmailStatus?)email.Status).FirstOrDefault()))
            .ToArrayAsync(cancellationToken);
        return new(Summary(await db.Clubs.SingleAsync(value => value.Id == clubId, cancellationToken)), items.Take(PageSize).ToArray(), page, items.Length > PageSize);
    }

    internal Task<ClubOperationOutcome> InviteAsync(ClaimsPrincipal actor, Guid clubId, InvitationInput input, Uri origin, CancellationToken cancellationToken) =>
        WriteAsync(actor, async (db, id, ct) =>
        {
            await RequireAdministratorAsync(db, id, clubId, ct);
            if (input.Id == Guid.Empty || !Enum.IsDefined(input.Role) ||
                !Validator.TryValidateObject(input, new ValidationContext(input), null, validateAllProperties: true))
            {
                return new ClubOperationOutcome.Invalid("Enter a valid email address and staff role.");
            }
            var normalized = input.Email.Trim().ToUpperInvariant();
            var existing = await db.ClubInvitations.SingleOrDefaultAsync(value => value.Id == input.Id, ct);
            if (existing is not null)
            {
                return existing.ClubId == clubId && string.Equals(existing.CreatedById, id, StringComparison.Ordinal) &&
                    string.Equals(existing.NormalizedEmail, normalized, StringComparison.Ordinal) && existing.Role == input.Role
                    ? new ClubOperationOutcome.Saved("This invitation was already created. Check its current delivery status.")
                    : new ClubOperationOutcome.Conflict("This invitation changed. Refresh before trying again.");
            }
            if (await db.ClubMemberships.Join(db.Users, member => member.UserId, user => user.Id, (member, user) => new { member.ClubId, user.NormalizedEmail })
                .AnyAsync(value => value.ClubId == clubId && value.NormalizedEmail == normalized, ct))
            {
                return new ClubOperationOutcome.Conflict("This person already belongs to your club. Change their role from People.");
            }
            var now = time.GetUtcNow();
            if (!await CanQueueInvitationAsync(db, clubId, ct))
            {
                return new ClubOperationOutcome.Invalid("This club has reached the hourly invitation limit. Try again later.");
            }
            if (await db.ClubInvitations.AnyAsync(value => value.ClubId == clubId && value.NormalizedEmail == normalized &&
                value.UsedAt == null && value.RevokedAt == null && value.ExpiresAt > now, ct))
            {
                return new ClubOperationOutcome.Conflict("There is already an active invitation for this email. Resend or revoke it from the list.");
            }
            var invitation = new ClubInvitation
            {
                Id = input.Id,
                ClubId = clubId,
                Email = input.Email.Trim(),
                NormalizedEmail = normalized,
                Role = input.Role,
                CreatedById = id,
                CreatedAt = now,
            };
            QueueInvitation(db, invitation, await db.Clubs.SingleAsync(value => value.Id == clubId, ct), origin);
            db.ClubInvitations.Add(invitation);
            return new ClubOperationOutcome.Saved("Invitation created and email queued. The link expires in seven days.");
        }, cancellationToken);

    internal Task<ClubOperationOutcome> ChangeInvitationAsync(ClaimsPrincipal actor, Guid clubId, InvitationChangeInput input, bool resend, Uri origin, CancellationToken cancellationToken) =>
        WriteAsync(actor, async (db, id, ct) =>
        {
            await RequireAdministratorAsync(db, id, clubId, ct);
            var invitation = await db.ClubInvitations.SingleOrDefaultAsync(value => value.ClubId == clubId && value.Id == input.Id, ct);
            if (invitation is null || invitation.Revision != input.Revision || invitation.UsedAt is not null || invitation.RevokedAt is not null)
            {
                return new ClubOperationOutcome.Conflict("This invitation changed, was used, or was revoked. Refresh the list.");
            }
            if (resend)
            {
                var recent = time.GetUtcNow().AddMinutes(-1);
                if (!await CanQueueInvitationAsync(db, clubId, ct) ||
                    await db.StaffEmails.AnyAsync(value => value.InvitationId == invitation.Id && value.CreatedAt > recent, ct))
                {
                    return new ClubOperationOutcome.Invalid("Wait at least one minute between resends. Clubs can send up to 50 invitation emails per hour.");
                }
            }
            await CancelInvitationMailAsync(db, invitation.Id, ct);
            invitation.Revision++;
            if (resend)
            {
                QueueInvitation(db, invitation, await db.Clubs.SingleAsync(value => value.Id == clubId, ct), origin);
                return new ClubOperationOutcome.Saved("A new invitation email is queued. Earlier links no longer work.");
            }
            invitation.RevokedAt = time.GetUtcNow();
            invitation.TokenHash = [];
            return new ClubOperationOutcome.Saved("Invitation revoked. It can no longer grant access.");
        }, cancellationToken);

    private async Task<bool> CanQueueInvitationAsync(ApplicationDbContext db, Guid clubId, CancellationToken cancellationToken)
    {
        var since = time.GetUtcNow().AddHours(-1);
        return await db.StaffEmails.CountAsync(value => value.ClubId == clubId && value.InvitationId != null && value.CreatedAt > since, cancellationToken) < 50;
    }

    private void QueueInvitation(ApplicationDbContext db, ClubInvitation invitation, Club club, Uri origin)
    {
        var token = InvitationTokens.Create();
        var url = mail.InvitationUrl(origin, invitation.Id, token);
        invitation.TokenHash = InvitationTokens.Hash(token);
        invitation.ExpiresAt = time.GetUtcNow().AddDays(7);
        var email = mail.Compose(club.Id, invitation.Email, null, "Your Pino staff invitation",
            $"You have been invited to {club.Name} as {invitation.Role}.\n\nPino accounts are for adults acting as club staff. Register or sign in with this email address, verify it, and complete your staff profile before accepting.\n\n{url}\n\nThis single-use link expires in seven days. If you did not expect this invitation, you can ignore it.");
        email.InvitationId = invitation.Id;
        email.InvitationRevision = invitation.Revision;
        db.StaffEmails.Add(email);
    }

    private static async Task CancelInvitationMailAsync(ApplicationDbContext db, Guid invitationId, CancellationToken cancellationToken)
    {
        var messages = await db.StaffEmails.Where(value => value.InvitationId == invitationId &&
            (value.Status == StaffEmailStatus.Pending || value.Status == StaffEmailStatus.Sending || value.Status == StaffEmailStatus.Failed)).ToListAsync(cancellationToken);
        foreach (var message in messages)
        {
            message.Status = StaffEmailStatus.Cancelled;
            message.ProtectedBody = "";
            message.LeaseId = null;
            message.LeaseUntil = null;
            message.Revision++;
        }
    }

    internal async Task<InvitationPreview> PreviewInvitationAsync(ClaimsPrincipal actor, Guid invitationId, string token, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var id = await VerifiedActorAsync(db, actor, cancellationToken);
        var invitation = await FindInvitationAsync(db, id, invitationId, token, cancellationToken);
        if (invitation is null)
        {
            return new(null, null, "This invitation is unavailable. Sign in with the invited, verified email address, or ask a club administrator for a new invitation.", CanAccept: false);
        }
        var club = Summary(await db.Clubs.SingleAsync(value => value.Id == invitation.ClubId, cancellationToken));
        if (invitation.UsedAt is not null)
        {
            return new(club, invitation.Role, "This invitation has already been used. Your current club access determines what you can do.", CanAccept: false);
        }
        var blocked = await CanChooseClubAsync(db, id, cancellationToken);
        var message = blocked?.Match(saved => saved.Message, invalid => invalid.Message, forbidden => forbidden.Message, conflict => conflict.Message)
            ?? "Accept to join this club with the role shown.";
        return new(club, invitation.Role, message, blocked is null);
    }

    internal Task<ClubOperationOutcome> AcceptInvitationAsync(ClaimsPrincipal actor, Guid invitationId, InvitationAcceptInput input, CancellationToken cancellationToken) =>
        WriteAsync(actor, async (db, id, ct) =>
        {
            var invitation = await FindInvitationAsync(db, id, invitationId, input.Token, ct);
            if (invitation is null)
            {
                return new ClubOperationOutcome.Invalid("This invitation is unavailable. Check your verified email address or request a new invitation.");
            }
            if (invitation.UsedAt is not null)
            {
                return new ClubOperationOutcome.Conflict("This invitation has already been used. It cannot grant access again.");
            }
            var blocked = await CanChooseClubAsync(db, id, ct);
            if (blocked is not null) { return blocked; }
            invitation.UsedAt = time.GetUtcNow();
            invitation.UsedById = id;
            invitation.Revision++;
            await CancelInvitationMailAsync(db, invitation.Id, ct);
            db.ClubMemberships.Add(new() { UserId = id, ClubId = invitation.ClubId, Role = invitation.Role, JoinedAt = time.GetUtcNow() });
            return new ClubOperationOutcome.Saved($"Invitation accepted. You joined as {invitation.Role}.");
        }, cancellationToken);

    private async Task<ClubInvitation?> FindInvitationAsync(ApplicationDbContext db, string userId, Guid invitationId, string token, CancellationToken cancellationToken)
    {
        var invitation = await db.ClubInvitations.SingleOrDefaultAsync(value => value.Id == invitationId, cancellationToken);
        if (invitation is null || invitation.RevokedAt is not null || invitation.ExpiresAt <= time.GetUtcNow() ||
            !InvitationTokens.Matches(token, invitation.TokenHash))
        {
            return null;
        }
        var email = await db.Users.Where(value => value.Id == userId).Select(value => value.NormalizedEmail).SingleAsync(cancellationToken);
        return string.Equals(email, invitation.NormalizedEmail, StringComparison.Ordinal) ? invitation : null;
    }
}
