using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pino.Data;

namespace Pino.Features.Clubs.Services;

// Identity deletion shares the access-transition lock: approval cannot race past the
// membership guard. Photo cleanup commits atomically with account deletion.
internal sealed class ClubUserStore(ApplicationDbContext context, IdentityErrorDescriber errors, TimeProvider time)
    : UserOnlyStore<ApplicationUser, ApplicationDbContext>(context, errors)
{
    public override async Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        return await Context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
            await Context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(731923981)", cancellationToken);
            if (await Context.ClubMemberships.AnyAsync(member => member.UserId == user.Id, cancellationToken))
            {
                return IdentityResult.Failed(new IdentityError { Code = "ClubMembership", Description = "Leave your club from Your club before deleting your account. If you are the last administrator, promote another member first." });
            }
            var profile = await Context.ClubProfiles.SingleOrDefaultAsync(value => value.UserId == user.Id, cancellationToken);
            var invitations = await Context.ClubInvitations.Where(value => value.UsedById == user.Id ||
                (user.NormalizedEmail != null && value.NormalizedEmail == user.NormalizedEmail)).ToArrayAsync(cancellationToken);
            Context.ClubInvitations.RemoveRange(invitations);
            if (profile?.PhotoKey is { } photoKey)
            {
                Context.PhotoDeletions.Add(new() { PhotoKey = photoKey, NotBefore = time.GetUtcNow() });
            }
            var result = await base.DeleteAsync(user, cancellationToken);
            if (result.Succeeded)
            {
                await transaction.CommitAsync(cancellationToken);
            }
            return result;
        });
    }
}
