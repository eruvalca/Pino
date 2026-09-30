namespace Pino.Features.Sporting.Data;

internal sealed class TryoutTeamAvailability
{
    public Guid ClubId { get; set; }
    public Guid TryoutId { get; set; }
    public Guid TeamId { get; set; }
    public bool Excluded { get; set; }
    public long Revision { get; set; }
}
