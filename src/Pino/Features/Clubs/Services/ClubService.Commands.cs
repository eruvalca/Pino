using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Data;
using Pino.Features.Clubs.Data;
using Pino.Features.Clubs.Models;
using Pino.SharedKernel.Clubs;

namespace Pino.Features.Clubs.Services;

internal sealed partial class ClubService
{
    private static async Task<ClubOperationOutcome?> CanChooseClubAsync(ApplicationDbContext db, string id, CancellationToken cancellationToken)
    {
        if (!await db.ClubProfiles.AnyAsync(value => value.UserId == id && value.PhotoKey != null && value.FirstName != "" && value.LastName != "", cancellationToken))
        {
            return new ClubOperationOutcome.Invalid("Complete your name and profile photo first.");
        }
        if (await db.ClubMemberships.AnyAsync(value => value.UserId == id, cancellationToken))
        {
            return new ClubOperationOutcome.Conflict("You already belong to a club. Leave it before choosing another.");
        }
        if (await db.ClubJoinRequests.AnyAsync(value => value.UserId == id && value.Status == JoinRequestStatus.Pending, cancellationToken))
        {
            return new ClubOperationOutcome.Conflict("You have a pending request. Check its status or cancel it first.");
        }
        return null;
    }

    internal Task<ClubOperationOutcome> CreateAsync(ClaimsPrincipal actor, CreateClubInput input, CancellationToken cancellationToken) =>
        WriteAsync(actor, async (db, id, ct) =>
        {
            if (!ClubRules.ValidClub(input))
            {
                return new ClubOperationOutcome.Invalid("Enter a club name, sport, city, and valid US state within the displayed limits.");
            }
            var existing = await db.Clubs.SingleOrDefaultAsync(value => value.CreatedBy == id && value.OperationId == input.OperationId, ct);
            if (existing is not null)
            {
                return new ClubOperationOutcome.Saved("This club was already created. Your current access is shown below.");
            }
            var blocked = await CanChooseClubAsync(db, id, ct);
            if (blocked is not null)
            {
                return blocked;
            }
            var club = new Club { Id = Guid.NewGuid(), Name = input.Name.Trim(), Sport = input.Sport.Trim(), City = input.City.Trim(), State = input.State, CreatedBy = id, OperationId = input.OperationId };
            db.Clubs.Add(club);
            db.ClubMemberships.Add(new() { UserId = id, ClubId = club.Id, Role = ClubRole.Administrator, JoinedAt = time.GetUtcNow() });
            return new ClubOperationOutcome.Saved("Club created. You are its first administrator.");
        }, cancellationToken);

    internal Task<ClubOperationOutcome> RequestAsync(ClaimsPrincipal actor, Guid clubId, CancellationToken cancellationToken) =>
        WriteAsync(actor, async (db, id, ct) =>
        {
            var blocked = await CanChooseClubAsync(db, id, ct);
            if (blocked is not null)
            {
                return blocked;
            }
            if (!await db.Clubs.AnyAsync(value => value.Id == clubId, ct))
            {
                return new ClubOperationOutcome.Invalid("That club is no longer available. Search again.");
            }
            var recent = time.GetUtcNow().AddHours(-1);
            if (await db.ClubJoinRequests.CountAsync(value => value.UserId == id && value.CreatedAt > recent, ct) >= 5)
            {
                return new ClubOperationOutcome.Invalid("You've made several join requests recently. Wait an hour before requesting again.");
            }
            db.ClubJoinRequests.Add(new() { Id = Guid.NewGuid(), UserId = id, ClubId = clubId, Status = JoinRequestStatus.Pending, CreatedAt = time.GetUtcNow() });
            var club = await db.Clubs.SingleAsync(value => value.Id == clubId, ct);
            var administrators = await db.ClubMemberships.Where(value => value.ClubId == clubId && value.Role == ClubRole.Administrator)
                .Join(db.Users.Where(user => user.EmailConfirmed && user.Email != null), member => member.UserId, user => user.Id,
                    (member, user) => new { user.Id, user.Email }).ToArrayAsync(ct);
            foreach (var administrator in administrators)
            {
                db.StaffEmails.Add(mail.Compose(clubId, administrator.Email!, administrator.Id, "New Pino club join request",
                    $"A staff member has asked to join {club.Name}. Sign in to Pino and open People & requests to review their profile and decide."));
            }
            return new ClubOperationOutcome.Saved("Request sent. A club administrator will review it.");
        }, cancellationToken);

    internal Task<ClubOperationOutcome> CancelAsync(ClaimsPrincipal actor, Guid requestId, CancellationToken cancellationToken) =>
        WriteAsync(actor, async (db, id, ct) =>
        {
            var request = await db.ClubJoinRequests.SingleOrDefaultAsync(value => value.Id == requestId && value.UserId == id, ct);
            if (request is null || request.Status != JoinRequestStatus.Pending)
            {
                return new ClubOperationOutcome.Conflict("This request is no longer pending. Check your current access.");
            }
            request.Status = JoinRequestStatus.Cancelled;
            request.DecidedAt = time.GetUtcNow();
            return new ClubOperationOutcome.Saved("Request cancelled. You can choose another club.");
        }, cancellationToken);

    internal Task<ClubOperationOutcome> DecideAsync(ClaimsPrincipal actor, Guid clubId, RequestDecisionInput input, CancellationToken cancellationToken) =>
        WriteAsync(actor, async (db, id, ct) =>
        {
            await RequireAdministratorAsync(db, id, clubId, ct);
            var request = await db.ClubJoinRequests.SingleOrDefaultAsync(value => value.Id == input.RequestId && value.ClubId == clubId, ct);
            if (request is null || request.Status != JoinRequestStatus.Pending)
            {
                return new ClubOperationOutcome.Conflict("This request was already handled. Refresh the request list.");
            }
            if (input.Approve)
            {
                if (await db.ClubMemberships.AnyAsync(value => value.UserId == request.UserId, ct))
                {
                    return new ClubOperationOutcome.Conflict("This person already belongs to a club. Access was not granted.");
                }
                db.ClubMemberships.Add(new() { UserId = request.UserId, ClubId = clubId, Role = ClubRole.Coach, JoinedAt = time.GetUtcNow() });
            }
            request.Status = input.Approve ? JoinRequestStatus.Approved : JoinRequestStatus.Denied;
            request.DecidedAt = time.GetUtcNow();
            var recipient = await db.Users.SingleAsync(value => value.Id == request.UserId, ct);
            if (recipient.EmailConfirmed && recipient.Email is { } email)
            {
                var club = await db.Clubs.SingleAsync(value => value.Id == clubId, ct);
                var outcome = input.Approve ? "approved. You now have coach access" : "denied. You may apply again";
                db.StaffEmails.Add(mail.Compose(clubId, email, recipient.Id, "Your Pino club join request",
                    $"Your request to join {club.Name} was {outcome}. Sign in to Pino to see your current access."));
            }
            return new ClubOperationOutcome.Saved(input.Approve ? "Approved as coach." : "Request denied. This person may reapply.");
        }, cancellationToken);

    internal Task<ClubOperationOutcome> ChangeMemberAsync(ClaimsPrincipal actor, Guid clubId, MemberChangeInput input, CancellationToken cancellationToken) =>
        WriteAsync(actor, async (db, id, ct) =>
        {
            await RequireAdministratorAsync(db, id, clubId, ct);
            return await ChangeMemberCoreAsync(db, clubId, input, ct);
        }, cancellationToken);

    private static async Task<ClubOperationOutcome> ChangeMemberCoreAsync(ApplicationDbContext db, Guid clubId, MemberChangeInput input, CancellationToken cancellationToken)
    {
        var member = await db.ClubMemberships.SingleOrDefaultAsync(value => value.UserId == input.UserId && value.ClubId == clubId, cancellationToken);
        if (member is null)
        {
            return new ClubOperationOutcome.Conflict("This person is no longer a member. Refresh the list.");
        }
        var count = await db.ClubMemberships.CountAsync(value => value.ClubId == clubId && value.Role == ClubRole.Administrator, cancellationToken);
        var error = ClubRules.MemberChangeError(member.Role, input.ExpectedRole, input.NewRole, count);
        if (error is not null)
        {
            return new ClubOperationOutcome.Conflict(error);
        }
        if (input.NewRole is { } role)
        {
            member.Role = role;
            return new ClubOperationOutcome.Saved($"Role changed to {role}.");
        }
        db.ClubMemberships.Remove(member);
        return new ClubOperationOutcome.Saved("Club access ended. The Pino account and club records are kept.");
    }

    internal Task<ClubOperationOutcome> LeaveAsync(ClaimsPrincipal actor, Guid clubId, CancellationToken cancellationToken) =>
        WriteAsync(actor, async (db, id, ct) =>
        {
            var member = await db.ClubMemberships.SingleOrDefaultAsync(value => value.UserId == id && value.ClubId == clubId, ct);
            return member is null
                ? new ClubOperationOutcome.Conflict("You no longer belong to this club.")
                : await ChangeMemberCoreAsync(db, clubId, new(id, member.Role, NewRole: null), ct);
        }, cancellationToken);
}
