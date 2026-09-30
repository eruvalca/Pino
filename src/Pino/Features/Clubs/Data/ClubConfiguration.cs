using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pino.Features.Clubs.Data;

internal sealed class ClubConfiguration : IEntityTypeConfiguration<Club>
{
    public void Configure(EntityTypeBuilder<Club> builder)
    {
        builder.Property(value => value.Name).HasMaxLength(120);
        builder.Property(value => value.Sport).HasMaxLength(60);
        builder.Property(value => value.City).HasMaxLength(100);
        builder.Property(value => value.State).HasMaxLength(2);
        builder.Property(value => value.CreatedBy).HasMaxLength(450);
        builder.Property(value => value.Revision).IsConcurrencyToken();
        builder.HasIndex(value => new { value.CreatedBy, value.OperationId }).IsUnique();
    }
}
