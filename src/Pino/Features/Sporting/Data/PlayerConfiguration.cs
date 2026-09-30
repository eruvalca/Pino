using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pino.Features.Clubs.Data;

namespace Pino.Features.Sporting.Data;

internal sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasAlternateKey(value => new { value.ClubId, value.Id });
        builder.HasOne<Club>().WithMany().HasForeignKey(value => value.ClubId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.PlayerReference).HasMaxLength(40);
        builder.Property(value => value.FirstName).HasMaxLength(80);
        builder.Property(value => value.MiddleName).HasMaxLength(80);
        builder.Property(value => value.LastName).HasMaxLength(80);
        builder.Property(value => value.Position).HasMaxLength(80);
        builder.Property(value => value.SecondaryPosition).HasMaxLength(80);
        builder.Property(value => value.ContactEmail).HasMaxLength(254);
        builder.Property(value => value.PhotoKey).HasMaxLength(100);
        builder.HasIndex(value => new { value.ClubId, value.PlayerReference }).IsUnique();
        builder.Property(value => value.Revision).IsConcurrencyToken();
        builder.ToTable(table => table.HasCheckConstraint("CK_Player_Year", "\"GraduationYear\" BETWEEN 2000 AND 2100"));
    }
}
