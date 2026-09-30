using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Pino.Features.Clubs.Services;

internal sealed partial class ClubService
{
    internal async Task<byte[]> ExportAsync(ClaimsPrincipal actor, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var id = await VerifiedActorAsync(db, actor, cancellationToken);
        var profile = await db.ClubProfiles.AsNoTracking().SingleOrDefaultAsync(value => value.UserId == id, cancellationToken);
        var membership = await db.ClubMemberships.AsNoTracking().SingleOrDefaultAsync(value => value.UserId == id, cancellationToken);
        var requests = await db.ClubJoinRequests.AsNoTracking().Where(value => value.UserId == id).OrderBy(value => value.CreatedAt).ToArrayAsync(cancellationToken);
        var photo = profile?.PhotoKey is { } key ? Convert.ToBase64String(await photos.DownloadAsync(key, cancellationToken)) : null;
        var email = await db.Users.Where(value => value.Id == id).Select(value => value.NormalizedEmail).SingleAsync(cancellationToken);
        var invitations = await db.ClubInvitations.AsNoTracking().Where(value => value.UsedById == id || (email != null && value.NormalizedEmail == email))
            .Select(value => new { value.ClubId, value.Email, value.Role, value.CreatedAt, value.ExpiresAt, value.UsedAt, value.RevokedAt }).ToArrayAsync(cancellationToken);
        var deliveries = await db.StaffEmails.AsNoTracking().Where(value => value.RecipientId == id || (email != null && value.NormalizedRecipientEmail == email))
            .Select(value => new { value.RecipientEmail, value.Subject, value.Status, value.CreatedAt, value.SentAt }).ToArrayAsync(cancellationToken);
        return JsonSerializer.SerializeToUtf8Bytes(new { profile?.FirstName, profile?.LastName, PhotoJpegBase64 = photo, Membership = membership, Requests = requests, Invitations = invitations, EmailDelivery = deliveries });
    }
}
