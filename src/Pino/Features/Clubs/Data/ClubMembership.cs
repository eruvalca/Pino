using Pino.SharedKernel.Clubs;

namespace Pino.Features.Clubs.Data;

internal sealed class ClubMembership
{
    public string UserId { get; set; } = "";
    public Guid ClubId { get; set; }
    public ClubRole Role { get; set; }
    public DateTimeOffset JoinedAt { get; set; }
}
