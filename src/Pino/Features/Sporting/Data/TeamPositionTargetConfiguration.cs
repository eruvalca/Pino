using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pino.Features.Sporting.Data;

internal sealed class TeamPositionTargetConfiguration : IEntityTypeConfiguration<TeamPositionTarget>
{
    public void Configure(EntityTypeBuilder<TeamPositionTarget> builder)
    {
        builder.HasKey(value => new { value.ClubId, value.TeamId, value.PositionKey });
        builder.HasOne<SportTeam>().WithMany(value => value.PositionTargets).HasForeignKey(value => new { value.ClubId, value.TeamId }).HasPrincipalKey(value => new { value.ClubId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.PositionKey).HasMaxLength(80);
        builder.Property(value => value.Position).HasMaxLength(80);
        builder.ToTable(table => table.HasCheckConstraint("CK_PositionTarget_Count", "\"Players\" BETWEEN 0 AND 2000"));
    }
}
