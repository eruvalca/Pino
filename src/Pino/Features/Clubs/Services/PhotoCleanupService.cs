using Azure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Pino.Data;

namespace Pino.Features.Clubs.Services;

internal sealed partial class PhotoCleanupService(IDbContextFactory<ApplicationDbContext> factory,
    IProfilePhotoStore photos, TimeProvider time, ILogger<PhotoCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1), time);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CleanAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is RequestFailedException or DbUpdateException or Npgsql.NpgsqlException or RetryLimitExceededException)
            {
                LogCleanupFailed(logger, exception);
            }
        }
    }

    private async Task CleanAsync(CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var now = time.GetUtcNow();
        var entries = await db.PhotoDeletions.Where(value => value.NotBefore <= now).Take(50).ToListAsync(cancellationToken);
        foreach (var entry in entries)
        {
            if (!await db.ClubProfiles.AnyAsync(value => value.PhotoKey == entry.PhotoKey, cancellationToken) &&
                !await db.Players.AnyAsync(value => value.PhotoKey == entry.PhotoKey, cancellationToken))
            {
                await photos.DeleteAsync(entry.PhotoKey, cancellationToken);
            }
            db.PhotoDeletions.Remove(entry);
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "Profile photo cleanup failed; pending deletions will be retried.")]
    private static partial void LogCleanupFailed(ILogger logger, Exception exception);
}
