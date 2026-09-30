using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pino.Features.Clubs.Data;

internal sealed class ClubInvitationConfiguration : IEntityTypeConfiguration<ClubInvitation>
{
    public void Configure(EntityTypeBuilder<ClubInvitation> builder)
    {
        builder.HasAlternateKey(value => new { value.ClubId, value.Id });
        builder.HasOne<Club>().WithMany().HasForeignKey(value => value.ClubId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.Email).HasMaxLength(254);
        builder.Property(value => value.NormalizedEmail).HasMaxLength(254);
        builder.Property(value => value.TokenHash).HasMaxLength(32);
        builder.Property(value => value.CreatedById).HasMaxLength(450);
        builder.Property(value => value.UsedById).HasMaxLength(450);
        builder.Property(value => value.Revision).IsConcurrencyToken();
        builder.HasIndex(value => new { value.ClubId, value.NormalizedEmail });
        builder.ToTable(table => table.HasCheckConstraint("CK_ClubInvitation_Role", "\"Role\" IN (0, 1)"));
    }
}
