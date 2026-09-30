using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pino.Features.Sporting.Data;

internal sealed class PlayerNoteConfiguration : IEntityTypeConfiguration<PlayerNote>
{
    public void Configure(EntityTypeBuilder<PlayerNote> builder)
    {
        builder.HasOne<Participation>().WithMany().HasForeignKey(value => new { value.ClubId, value.TryoutId, value.PlayerId }).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.Text).HasMaxLength(4000);
        builder.Property(value => value.AuthorId).HasMaxLength(450);
        builder.Property(value => value.Author).HasMaxLength(161);
        builder.Property(value => value.RedactedById).HasMaxLength(450);
        builder.Property(value => value.RedactedBy).HasMaxLength(161);
        builder.Property(value => value.RedactionReason).HasMaxLength(1000);
        builder.HasIndex(value => new { value.ClubId, value.RedactionOperationId });
        builder.HasIndex(value => value.CorrectsId).IsUnique().HasFilter("\"CorrectsId\" IS NOT NULL");
    }
}
