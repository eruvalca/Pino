using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pino.Features.Sporting.Data;

internal sealed class ParticipationConfiguration : IEntityTypeConfiguration<Participation>
{
    public void Configure(EntityTypeBuilder<Participation> builder)
    {
        builder.HasKey(value => new { value.ClubId, value.TryoutId, value.PlayerId });
        builder.HasOne<Player>().WithMany().HasForeignKey(value => new { value.ClubId, value.PlayerId }).HasPrincipalKey(value => new { value.ClubId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TryoutEvent>().WithMany().HasForeignKey(value => new { value.ClubId, value.TryoutId }).HasPrincipalKey(value => new { value.ClubId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SportTeam>().WithMany().HasForeignKey(value => new { value.ClubId, value.TeamId }).HasPrincipalKey(value => new { value.ClubId, value.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.Bib).HasMaxLength(20);
        builder.HasIndex(value => new { value.ClubId, value.TryoutId, value.Bib }).IsUnique().HasFilter("\"Bib\" <> '' AND NOT \"Removed\"");
        builder.Property(value => value.Revision).IsConcurrencyToken();
        builder.ToTable(table => table.HasCheckConstraint("CK_Participation_Decision", "\"Decision\" BETWEEN 0 AND 4 AND ((\"Decision\" = 1) = (\"TeamId\" IS NOT NULL))"));
    }
}
