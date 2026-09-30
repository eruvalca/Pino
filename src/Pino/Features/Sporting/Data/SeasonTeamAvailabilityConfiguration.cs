using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pino.Features.Sporting.Data;

internal sealed class SeasonTeamAvailabilityConfiguration : IEntityTypeConfiguration<SeasonTeamAvailability>
{
    public void Configure(EntityTypeBuilder<SeasonTeamAvailability> builder)
    {
        builder.HasKey(value => new { value.ClubId, value.SeasonId, value.TeamId });
        builder.HasOne<Season>().WithMany().HasForeignKey(value => new { value.ClubId, value.SeasonId }).HasPrincipalKey(value => new { value.ClubId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SportTeam>().WithMany().HasForeignKey(value => new { value.ClubId, value.TeamId }).HasPrincipalKey(value => new { value.ClubId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.Revision).IsConcurrencyToken();
    }
}
