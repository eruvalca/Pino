using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Features.Clubs.Data;
using Pino.Features.Clubs.Models;
using Pino.SharedKernel.Clubs;

namespace Pino.Features.Clubs.Services;

internal sealed partial class ClubService
{
    internal async Task<ClubOperationOutcome> SaveProfileAsync(ClaimsPrincipal actor, ProfileInput input, CancellationToken cancellationToken)
    {
        if (!ClubRules.ValidProfile(input))
        {
            return new ClubOperationOutcome.Invalid("Enter your first and last names, up to 80 characters each.");
        }
        string? photoKey = null;
        if (input.CroppedPhoto is not null)
        {
            byte[] jpeg;
            try
            {
                jpeg = ProfilePhoto.Normalize(input.CroppedPhoto);
            }
            catch (Exception exception) when (exception is FormatException or InvalidDataException)
            {
                return new ClubOperationOutcome.Invalid("The photo could not be read. Choose a JPEG or PNG and crop it again.");
            }
            photoKey = $"{Guid.NewGuid():N}.jpg";
            // Register cleanup before uploading so a crash cannot leave an untracked private blob.
            await using var staging = await factory.CreateDbContextAsync(cancellationToken);
            await VerifiedActorAsync(staging, actor, cancellationToken);
            staging.PhotoDeletions.Add(new() { PhotoKey = photoKey, NotBefore = time.GetUtcNow().AddHours(1) });
            await staging.SaveChangesAsync(cancellationToken);
            await photos.UploadAsync(photoKey, jpeg, cancellationToken);
        }
        return await WriteAsync(actor, async (db, id, ct) =>
        {
            var profile = await db.ClubProfiles.SingleOrDefaultAsync(value => value.UserId == id, ct);
            if (profile?.PhotoKey is null && photoKey is null)
            {
                return new ClubOperationOutcome.Invalid("Add and crop a profile photo to continue.");
            }
            if (profile is null)
            {
                profile = new ClubProfile { UserId = id };
                db.ClubProfiles.Add(profile);
            }
            profile.FirstName = input.FirstName.Trim();
            profile.LastName = input.LastName.Trim();
            if (photoKey is not null)
            {
                if (profile.PhotoKey is { } oldKey && !string.Equals(oldKey, photoKey, StringComparison.Ordinal))
                {
                    db.PhotoDeletions.Add(new() { PhotoKey = oldKey, NotBefore = time.GetUtcNow() });
                }
                profile.PhotoKey = photoKey;
                var cleanup = await db.PhotoDeletions.FindAsync([photoKey], ct);
                if (cleanup is not null)
                {
                    db.PhotoDeletions.Remove(cleanup);
                }
            }
            return new ClubOperationOutcome.Saved("Profile saved.");
        }, cancellationToken);
    }
}
