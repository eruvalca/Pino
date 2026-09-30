using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pino.Features.Clubs.Data;

namespace Pino.Features.Sporting.Data;

internal sealed class SportingBatchReceiptConfiguration : IEntityTypeConfiguration<SportingBatchReceipt>
{
    public void Configure(EntityTypeBuilder<SportingBatchReceipt> builder)
    {
        builder.HasOne<Club>().WithMany().HasForeignKey(value => value.ClubId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.ActorId).HasMaxLength(450);
        builder.Property(value => value.Action).HasMaxLength(32);
    }
}
