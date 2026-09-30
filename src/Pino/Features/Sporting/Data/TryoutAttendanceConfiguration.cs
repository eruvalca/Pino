using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Pino.Features.Sporting.Data;

internal sealed class TryoutAttendanceConfiguration : IEntityTypeConfiguration<TryoutAttendance>
{
    public void Configure(EntityTypeBuilder<TryoutAttendance> builder)
    {
        builder.HasKey(value => new { value.ClubId, value.TryoutId, value.PlayerId });
        builder.HasOne<Participation>().WithMany().HasForeignKey(value => new { value.ClubId, value.TryoutId, value.PlayerId }).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.RecordedById).HasMaxLength(450);
        builder.Property(value => value.RecordedBy).HasMaxLength(161);
        builder.Property(value => value.Revision).IsConcurrencyToken();
        builder.ToTable(table => table.HasCheckConstraint("CK_TryoutAttendance_Kind", "\"Kind\" BETWEEN 0 AND 2"));
    }
}
