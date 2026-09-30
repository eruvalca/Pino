using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pino.Features.Clubs.Data;
using Pino.Features.Sporting.Data;

namespace Pino.Data;

internal sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    internal DbSet<Club> Clubs => Set<Club>();
    internal DbSet<ClubProfile> ClubProfiles => Set<ClubProfile>();
    internal DbSet<ClubMembership> ClubMemberships => Set<ClubMembership>();
    internal DbSet<ClubJoinRequest> ClubJoinRequests => Set<ClubJoinRequest>();
    internal DbSet<PhotoDeletion> PhotoDeletions => Set<PhotoDeletion>();
    internal DbSet<Player> Players => Set<Player>();
    internal DbSet<Season> Seasons => Set<Season>();
    internal DbSet<SportTeam> SportTeams => Set<SportTeam>();
    internal DbSet<TryoutEvent> TryoutEvents => Set<TryoutEvent>();
    internal DbSet<Participation> Participations => Set<Participation>();
    internal DbSet<SeasonPlacement> SeasonPlacements => Set<SeasonPlacement>();
    internal DbSet<PlayerNote> PlayerNotes => Set<PlayerNote>();
    internal DbSet<DecisionEvent> DecisionEvents => Set<DecisionEvent>();
    internal DbSet<TryoutCloseout> TryoutCloseouts => Set<TryoutCloseout>();
    internal DbSet<TryoutCloseoutPlayer> TryoutCloseoutPlayers => Set<TryoutCloseoutPlayer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
