using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pino.Features.Sporting.Data;

internal sealed class TryoutEventConfiguration : IEntityTypeConfiguration<TryoutEvent>
{
    public void Configure(EntityTypeBuilder<TryoutEvent> builder)
    {
        builder.HasAlternateKey(value => new { value.ClubId, value.Id });
        builder.HasOne<Season>().WithMany().HasForeignKey(value => new { value.ClubId, value.SeasonId }).HasPrincipalKey(value => new { value.ClubId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.Name).HasMaxLength(120);
        builder.Property(value => value.Location).HasMaxLength(160);
        builder.Property(value => value.Revision).IsConcurrencyToken();
    }
}
