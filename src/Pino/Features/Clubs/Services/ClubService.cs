using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Data;
using Pino.Features.Clubs.Models;

namespace Pino.Features.Clubs.Services;

internal sealed partial class ClubService(IDbContextFactory<ApplicationDbContext> factory, IProfilePhotoStore photos, TimeProvider time, ClubMail mail)
{
    private static string ActorId(ClaimsPrincipal actor) =>
        actor.Identity?.IsAuthenticated == true && actor.FindFirstValue(ClaimTypes.NameIdentifier) is { } id
            ? id : throw new UnauthorizedAccessException();

    private static async Task<string> VerifiedActorAsync(ApplicationDbContext db, ClaimsPrincipal actor, CancellationToken cancellationToken)
    {
        var id = ActorId(actor);
        if (!await db.Users.AnyAsync(user => user.Id == id && user.EmailConfirmed, cancellationToken))
        {
            throw new UnauthorizedAccessException();
        }
        return id;
    }

    private async Task<ClubOperationOutcome> WriteAsync(ClaimsPrincipal actor,
        Func<ApplicationDbContext, string, CancellationToken, Task<ClubOperationOutcome>> operation,
        CancellationToken cancellationToken)
    {
        await using var strategyContext = await factory.CreateDbContextAsync(cancellationToken);
        return await strategyContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var db = await factory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            // All membership/request transitions share this short database lock, including across
            // server instances. This protects absence checks and the last administrator together.
            // Photo I/O happens outside this transaction. Revisit lock granularity with measured load.
            await db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(731923981)", cancellationToken);
            var id = await VerifiedActorAsync(db, actor, cancellationToken);
            var outcome = await operation(db, id, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return outcome;
        });
    }
}
