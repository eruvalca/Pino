using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pino.Features.Sporting.Data;

internal sealed class DecisionEventConfiguration : IEntityTypeConfiguration<DecisionEvent>
{
    public void Configure(EntityTypeBuilder<DecisionEvent> builder)
    {
        builder.HasOne<Participation>().WithMany().HasForeignKey(value => new { value.ClubId, value.TryoutId, value.PlayerId }).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.TryoutName).HasMaxLength(120);
        builder.Property(value => value.SeasonName).HasMaxLength(120);
        builder.Property(value => value.TeamName).HasMaxLength(120);
        builder.Property(value => value.AuthorId).HasMaxLength(450);
        builder.Property(value => value.Author).HasMaxLength(161);
        builder.Property(value => value.Reason).HasMaxLength(1000);
        builder.HasIndex(value => new { value.ClubId, value.PlayerId, value.CreatedAt });
    }
}
