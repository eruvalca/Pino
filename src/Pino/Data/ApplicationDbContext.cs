using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pino.Features.Clubs.Data;

namespace Pino.Data;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    internal DbSet<Club> Clubs => Set<Club>();
    internal DbSet<ClubProfile> ClubProfiles => Set<ClubProfile>();
    internal DbSet<ClubMembership> ClubMemberships => Set<ClubMembership>();
    internal DbSet<ClubJoinRequest> ClubJoinRequests => Set<ClubJoinRequest>();
    internal DbSet<PhotoDeletion> PhotoDeletions => Set<PhotoDeletion>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ClubModel.Configure(builder);
    }
}
