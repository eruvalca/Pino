using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pino.Features.Clubs.Data;

internal sealed class PhotoDeletionConfiguration : IEntityTypeConfiguration<PhotoDeletion>
{
    public void Configure(EntityTypeBuilder<PhotoDeletion> builder)
    {
        builder.HasKey(value => value.PhotoKey);
        builder.Property(value => value.PhotoKey).HasMaxLength(100);
    }
}
