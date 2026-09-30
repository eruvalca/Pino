using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pino.Features.Clubs.Data;

namespace Pino.Features.Sporting.Data;

internal sealed class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.HasAlternateKey(value => new { value.ClubId, value.Id });
        builder.HasOne<Club>().WithMany().HasForeignKey(value => value.ClubId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.Name).HasMaxLength(120);
        builder.Property(value => value.Revision).IsConcurrencyToken();
        builder.ToTable(table => table.HasCheckConstraint("CK_Season_Dates", "\"StartsOn\" <= \"EndsOn\""));
    }
}
