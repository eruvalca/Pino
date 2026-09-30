using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pino.Features.Sporting.Data;

internal sealed class TryoutCloseoutConfiguration : IEntityTypeConfiguration<TryoutCloseout>
{
    public void Configure(EntityTypeBuilder<TryoutCloseout> builder)
    {
        builder.HasOne<TryoutEvent>().WithMany().HasForeignKey(value => new { value.ClubId, value.TryoutId }).HasPrincipalKey(value => new { value.ClubId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(value => value.Players).WithOne().HasForeignKey(value => value.CloseoutId).OnDelete(DeleteBehavior.Cascade);
        builder.Property(value => value.TryoutName).HasMaxLength(120);
        builder.Property(value => value.SeasonName).HasMaxLength(120);
        builder.Property(value => value.ClosedBy).HasMaxLength(161);
        builder.Property(value => value.ReopenedBy).HasMaxLength(161);
        builder.Property(value => value.ReopenReason).HasMaxLength(1000);
        builder.HasIndex(value => new { value.ClubId, value.TryoutId, value.ClosedAt });
    }
}
