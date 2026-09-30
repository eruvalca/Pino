namespace Pino.Features.Sporting.Data;

internal sealed class TeamPositionTarget
{
    public Guid ClubId { get; set; }
    public Guid TeamId { get; set; }
    public string PositionKey { get; set; } = "";
    public string Position { get; set; } = "";
    public int Players { get; set; }
}
