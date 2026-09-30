using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pino.Features.Clubs.Data;

namespace Pino.Features.Sporting.Data;

internal sealed class SportTeamConfiguration : IEntityTypeConfiguration<SportTeam>
{
    public void Configure(EntityTypeBuilder<SportTeam> builder)
    {
        builder.HasAlternateKey(value => new { value.ClubId, value.Id });
        builder.HasOne<Club>().WithMany().HasForeignKey(value => value.ClubId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.Name).HasMaxLength(120);
        builder.Property(value => value.Revision).IsConcurrencyToken();
        builder.ToTable(table => table.HasCheckConstraint("CK_Team_Year", "\"GraduationYear\" BETWEEN 2000 AND 2100"));
        builder.ToTable(table => table.HasCheckConstraint("CK_Team_RosterTarget", "\"RosterTarget\" IS NULL OR \"RosterTarget\" BETWEEN 0 AND 2000"));
    }
}
