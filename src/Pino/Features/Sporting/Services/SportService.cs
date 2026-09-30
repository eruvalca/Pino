using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Data;
using Pino.Features.Clubs.Services;
using Pino.Features.Sporting.Data;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService(IDbContextFactory<ApplicationDbContext> factory, IProfilePhotoStore photos, TimeProvider time)
{
    private const string StaleMessage = "This record changed since you opened it. Reload to review the latest version before saving.";

    private static async Task<string> RequireMemberAsync(ApplicationDbContext db, ClaimsPrincipal actor, Guid clubId, CancellationToken ct)
    {
        var id = actor.FindFirstValue(ClaimTypes.NameIdentifier);
        if (actor.Identity?.IsAuthenticated != true || id is null ||
            !await db.Users.AnyAsync(user => user.Id == id && user.EmailConfirmed, ct) ||
            !await db.ClubMemberships.AnyAsync(member => member.UserId == id && member.ClubId == clubId, ct))
        {
            throw new UnauthorizedAccessException();
        }
        return id;
    }

    private async Task<T> WriteAsync<T>(ClaimsPrincipal actor, Guid clubId,
        Func<ApplicationDbContext, string, CancellationToken, Task<T>> operation, CancellationToken ct)
    {
        await using var strategyContext = await factory.CreateDbContextAsync(ct);
        return await strategyContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var db = await factory.CreateDbContextAsync(ct);
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            // Share membership's lock so revocation cannot race a sporting write. Transactions
            // contain only database work; image uploads are staged outside the lock.
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(731923981)", ct);
            var id = await RequireMemberAsync(db, actor, clubId, ct);
            var result = await operation(db, id, ct);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return result;
        });
    }

    private static async Task<string> AuthorAsync(ApplicationDbContext db, string id, CancellationToken ct) =>
        await db.ClubProfiles.Where(profile => profile.UserId == id).Select(profile => profile.FirstName + " " + profile.LastName).SingleAsync(ct);

    private static Task<Season?> SeasonAsync(ApplicationDbContext db, Guid clubId, Guid id, CancellationToken ct) =>
        db.Seasons.SingleOrDefaultAsync(value => value.ClubId == clubId && value.Id == id, ct);

    private static Task<TryoutEvent?> TryoutAsync(ApplicationDbContext db, Guid clubId, Guid id, CancellationToken ct) =>
        db.TryoutEvents.SingleOrDefaultAsync(value => value.ClubId == clubId && value.Id == id, ct);
}
