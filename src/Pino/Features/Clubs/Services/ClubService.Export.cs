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
        return JsonSerializer.SerializeToUtf8Bytes(new { profile?.FirstName, profile?.LastName, PhotoJpegBase64 = photo, Membership = membership, Requests = requests });
    }
}
