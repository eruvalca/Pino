using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pino.Features.Clubs.Data;

namespace Pino.Features.Sporting.Data;

internal sealed class PlayerImportReceiptConfiguration : IEntityTypeConfiguration<PlayerImportReceipt>
{
    public void Configure(EntityTypeBuilder<PlayerImportReceipt> builder)
    {
        builder.HasOne<Club>().WithMany().HasForeignKey(value => value.ClubId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(value => value.ActorId).HasMaxLength(450);
    }
}
