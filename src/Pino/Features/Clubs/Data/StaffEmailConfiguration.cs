using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pino.Data;

namespace Pino.Features.Clubs.Data;

internal sealed class StaffEmailConfiguration : IEntityTypeConfiguration<StaffEmail>
{
    public void Configure(EntityTypeBuilder<StaffEmail> builder)
    {
        builder.HasOne<Club>().WithMany().HasForeignKey(value => value.ClubId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(value => value.RecipientId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ClubInvitation>().WithMany().HasForeignKey(value => new { value.ClubId, value.InvitationId }).HasPrincipalKey(value => new { value.ClubId, value.Id }).OnDelete(DeleteBehavior.Cascade);
        builder.Property(value => value.RecipientEmail).HasMaxLength(254);
        builder.Property(value => value.NormalizedRecipientEmail).HasMaxLength(254);
        builder.HasIndex(value => value.NormalizedRecipientEmail);
        builder.Property(value => value.Subject).HasMaxLength(200);
        builder.Property(value => value.Revision).IsConcurrencyToken();
        builder.HasIndex(value => new { value.Status, value.NextAttemptAt });
        builder.HasIndex(value => new { value.ClubId, value.CreatedAt });
        builder.ToTable(table => table.HasCheckConstraint("CK_StaffEmail_Status", "\"Status\" IN (0, 1, 2, 3, 4)"));
    }
}
