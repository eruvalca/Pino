using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pino.Features.Sporting.Data;

internal sealed class TryoutCloseoutPlayerConfiguration : IEntityTypeConfiguration<TryoutCloseoutPlayer>
{
    public void Configure(EntityTypeBuilder<TryoutCloseoutPlayer> builder)
    {
        builder.HasKey(value => new { value.CloseoutId, value.PlayerId });
        // Snapshot identity and team names are historical values, not live navigations.
        builder.Property(value => value.FirstName).HasMaxLength(80);
        builder.Property(value => value.LastName).HasMaxLength(80);
        builder.Property(value => value.Bib).HasMaxLength(20);
        builder.Property(value => value.TeamName).HasMaxLength(120);
    }
}
