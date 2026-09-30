using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pino.Features.Clubs.Data;

namespace Pino.Features.Sporting.Data;

internal sealed class PlayerErasureConfiguration : IEntityTypeConfiguration<PlayerErasure>
{
    public void Configure(EntityTypeBuilder<PlayerErasure> builder)
    {
        builder.HasOne<Club>().WithMany().HasForeignKey(value => value.ClubId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.ActorId).HasMaxLength(450);
        builder.HasOne<PhotoDeletion>().WithMany().HasForeignKey(value => value.PhotoKey).OnDelete(DeleteBehavior.SetNull);
    }
}
