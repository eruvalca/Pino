using Pino.SharedKernel.Clubs;

namespace Pino.Features.Clubs.Data;

internal sealed class StaffEmail
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string? RecipientId { get; set; }
    public string RecipientEmail { get; set; } = "";
    public string NormalizedRecipientEmail { get; set; } = "";
    public string Subject { get; set; } = "";
    public string ProtectedBody { get; set; } = "";
    public Guid? InvitationId { get; set; }
    public long? InvitationRevision { get; set; }
    public StaffEmailStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset NextAttemptAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public int Attempts { get; set; }
    public Guid? LeaseId { get; set; }
    public DateTimeOffset? LeaseUntil { get; set; }
    public long Revision { get; set; } = 1;
}
