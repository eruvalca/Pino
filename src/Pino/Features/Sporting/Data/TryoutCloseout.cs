namespace Pino.Features.Sporting.Data;

internal sealed class TryoutCloseout
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid TryoutId { get; set; }
    public string TryoutName { get; set; } = "";
    public string SeasonName { get; set; } = "";
    public DateOnly TryoutDate { get; set; }
    public DateTimeOffset ClosedAt { get; set; }
    public string ClosedBy { get; set; } = "";
    public DateTimeOffset? ReopenedAt { get; set; }
    public string? ReopenedBy { get; set; }
    public string? ReopenReason { get; set; }
    public List<TryoutCloseoutPlayer> Players { get; set; } = [];
}
