using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pino.Features.Sporting.Data;

internal sealed class SeasonPlacementConfiguration : IEntityTypeConfiguration<SeasonPlacement>
{
    public void Configure(EntityTypeBuilder<SeasonPlacement> builder)
    {
        builder.HasKey(value => new { value.ClubId, value.SeasonId, value.PlayerId });
        builder.HasOne<Player>().WithMany().HasForeignKey(value => new { value.ClubId, value.PlayerId }).HasPrincipalKey(value => new { value.ClubId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Season>().WithMany().HasForeignKey(value => new { value.ClubId, value.SeasonId }).HasPrincipalKey(value => new { value.ClubId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SportTeam>().WithMany().HasForeignKey(value => new { value.ClubId, value.TeamId }).HasPrincipalKey(value => new { value.ClubId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TryoutEvent>().WithMany().HasForeignKey(value => new { value.ClubId, value.TryoutId }).HasPrincipalKey(value => new { value.ClubId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.Revision).IsConcurrencyToken();
    }
}
