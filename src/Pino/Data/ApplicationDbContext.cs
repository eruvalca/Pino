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
    internal DbSet<ClubInvitation> ClubInvitations => Set<ClubInvitation>();
    internal DbSet<StaffEmail> StaffEmails => Set<StaffEmail>();
    internal DbSet<PhotoDeletion> PhotoDeletions => Set<PhotoDeletion>();
    internal DbSet<Player> Players => Set<Player>();
    internal DbSet<Season> Seasons => Set<Season>();
    internal DbSet<SportTeam> SportTeams => Set<SportTeam>();
    internal DbSet<SeasonTeamAvailability> SeasonTeamAvailabilities => Set<SeasonTeamAvailability>();
    internal DbSet<TryoutTeamAvailability> TryoutTeamAvailabilities => Set<TryoutTeamAvailability>();
    internal DbSet<TryoutEvent> TryoutEvents => Set<TryoutEvent>();
    internal DbSet<Participation> Participations => Set<Participation>();
    internal DbSet<EnrollmentChange> EnrollmentChanges => Set<EnrollmentChange>();
    internal DbSet<SeasonPlacement> SeasonPlacements => Set<SeasonPlacement>();
    internal DbSet<PlayerNote> PlayerNotes => Set<PlayerNote>();
    internal DbSet<DecisionEvent> DecisionEvents => Set<DecisionEvent>();
    internal DbSet<TryoutCloseout> TryoutCloseouts => Set<TryoutCloseout>();
    internal DbSet<TryoutCloseoutPlayer> TryoutCloseoutPlayers => Set<TryoutCloseoutPlayer>();
    internal DbSet<PlayerErasure> PlayerErasures => Set<PlayerErasure>();
    internal DbSet<PlayerImportReceipt> PlayerImportReceipts => Set<PlayerImportReceipt>();
    internal DbSet<SportingBatchReceipt> SportingBatchReceipts => Set<SportingBatchReceipt>();
    internal DbSet<TeamPositionTarget> TeamPositionTargets => Set<TeamPositionTarget>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
