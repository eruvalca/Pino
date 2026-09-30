using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pino.Data;

namespace Pino.Features.Clubs.Data;

internal sealed class ClubProfileConfiguration : IEntityTypeConfiguration<ClubProfile>
{
    public void Configure(EntityTypeBuilder<ClubProfile> builder)
    {
        builder.HasKey(value => value.UserId);
        builder.Property(value => value.FirstName).HasMaxLength(80);
        builder.Property(value => value.LastName).HasMaxLength(80);
        builder.Property(value => value.PhotoKey).HasMaxLength(100);
        builder.HasOne<ApplicationUser>().WithOne().HasForeignKey<ClubProfile>(value => value.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
