using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pino.Features.Sporting.Data;

internal sealed class TryoutTeamAvailabilityConfiguration : IEntityTypeConfiguration<TryoutTeamAvailability>
{
    public void Configure(EntityTypeBuilder<TryoutTeamAvailability> builder)
    {
        builder.HasKey(value => new { value.ClubId, value.TryoutId, value.TeamId });
        builder.HasOne<TryoutEvent>().WithMany().HasForeignKey(value => new { value.ClubId, value.TryoutId }).HasPrincipalKey(value => new { value.ClubId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SportTeam>().WithMany().HasForeignKey(value => new { value.ClubId, value.TeamId }).HasPrincipalKey(value => new { value.ClubId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.Revision).IsConcurrencyToken();
    }
}
