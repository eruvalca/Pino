using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pino.Data;

namespace Pino.Features.Clubs.Data;

internal sealed class ClubJoinRequestConfiguration : IEntityTypeConfiguration<ClubJoinRequest>
{
    public void Configure(EntityTypeBuilder<ClubJoinRequest> builder)
    {
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(value => value.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Club>().WithMany().HasForeignKey(value => value.ClubId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(value => value.UserId).IsUnique().HasFilter("\"Status\" = 0");
        builder.HasIndex(value => new { value.ClubId, value.Status, value.CreatedAt });
        builder.ToTable(table => table.HasCheckConstraint("CK_ClubJoinRequest_Status", "\"Status\" IN (0, 1, 2, 3)"));
    }
}
