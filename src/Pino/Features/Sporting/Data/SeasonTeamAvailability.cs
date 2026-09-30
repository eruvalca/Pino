namespace Pino.Features.Sporting.Data;

internal sealed class SeasonTeamAvailability
{
    public Guid ClubId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid TeamId { get; set; }
    public bool Excluded { get; set; }
    public long Revision { get; set; }
}
