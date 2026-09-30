using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Data;

internal sealed class TryoutAttendance
{
    public Guid ClubId { get; set; }
    public Guid TryoutId { get; set; }
    public Guid PlayerId { get; set; }
    public AttendanceKind Kind { get; set; }
    public long Revision { get; set; }
    public string RecordedById { get; set; } = "";
    public string RecordedBy { get; set; } = "";
    public DateTimeOffset RecordedAt { get; set; }
}
