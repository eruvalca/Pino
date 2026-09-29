using Pino.SharedKernel.Clubs;

namespace Pino.Features.Clubs.Data;

internal sealed class ClubJoinRequest
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public Guid ClubId { get; set; }
    public JoinRequestStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
}
