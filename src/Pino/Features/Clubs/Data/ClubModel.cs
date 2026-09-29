using Microsoft.EntityFrameworkCore;
using Pino.Data;

namespace Pino.Features.Clubs.Data;

internal static class ClubModel
{
    internal static void Configure(ModelBuilder builder)
    {
        var club = builder.Entity<Club>();
        club.Property(value => value.Name).HasMaxLength(120);
        club.Property(value => value.Sport).HasMaxLength(60);
        club.Property(value => value.City).HasMaxLength(100);
        club.Property(value => value.State).HasMaxLength(2);
        club.Property(value => value.CreatedBy).HasMaxLength(450);
        club.HasIndex(value => new { value.CreatedBy, value.OperationId }).IsUnique();

        var profile = builder.Entity<ClubProfile>();
        profile.HasKey(value => value.UserId);
        profile.Property(value => value.FirstName).HasMaxLength(80);
        profile.Property(value => value.LastName).HasMaxLength(80);
        profile.Property(value => value.PhotoKey).HasMaxLength(100);
        profile.HasOne<ApplicationUser>().WithOne().HasForeignKey<ClubProfile>(value => value.UserId).OnDelete(DeleteBehavior.Cascade);

        var member = builder.Entity<ClubMembership>();
        member.HasKey(value => value.UserId);
        member.HasIndex(value => new { value.ClubId, value.Role });
        // Deleting an Identity account must not bypass leaving/last-administrator protection.
        member.HasOne<ApplicationUser>().WithOne().HasForeignKey<ClubMembership>(value => value.UserId).OnDelete(DeleteBehavior.Restrict);
        member.HasOne<Club>().WithMany().HasForeignKey(value => value.ClubId).OnDelete(DeleteBehavior.Restrict);
        member.ToTable(table => table.HasCheckConstraint("CK_ClubMembership_Role", "\"Role\" IN (0, 1)"));

        var request = builder.Entity<ClubJoinRequest>();
        request.HasOne<ApplicationUser>().WithMany().HasForeignKey(value => value.UserId).OnDelete(DeleteBehavior.Cascade);
        request.HasOne<Club>().WithMany().HasForeignKey(value => value.ClubId).OnDelete(DeleteBehavior.Restrict);
        request.HasIndex(value => value.UserId).IsUnique().HasFilter("\"Status\" = 0");
        request.HasIndex(value => new { value.ClubId, value.Status, value.CreatedAt });
        request.ToTable(table => table.HasCheckConstraint("CK_ClubJoinRequest_Status", "\"Status\" IN (0, 1, 2, 3)"));

        builder.Entity<PhotoDeletion>().HasKey(value => value.PhotoKey);
        builder.Entity<PhotoDeletion>().Property(value => value.PhotoKey).HasMaxLength(100);
    }
}
