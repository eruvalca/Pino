using Pino.SharedKernel.Clubs;

namespace Pino.Features.Clubs.Data;

internal sealed class ClubInvitation
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string Email { get; set; } = "";
    public string NormalizedEmail { get; set; } = "";
    public byte[] TokenHash { get; set; } = [];
    public ClubRole Role { get; set; }
    public string CreatedById { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public string? UsedById { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public long Revision { get; set; } = 1;
}
