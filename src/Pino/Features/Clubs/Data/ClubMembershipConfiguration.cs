using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pino.Data;

namespace Pino.Features.Clubs.Data;

internal sealed class ClubMembershipConfiguration : IEntityTypeConfiguration<ClubMembership>
{
    public void Configure(EntityTypeBuilder<ClubMembership> builder)
    {
        builder.HasKey(value => value.UserId);
        builder.HasIndex(value => new { value.ClubId, value.Role });
        // Deleting an Identity account must not bypass leaving/last-administrator protection.
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<ClubMembership>(value => value.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Club>().WithMany().HasForeignKey(value => value.ClubId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table => table.HasCheckConstraint("CK_ClubMembership_Role", "\"Role\" IN (0, 1)"));
    }
}
