namespace Pino.Features.Sporting.Data;

internal sealed class SeasonPlacement
{
    public Guid ClubId { get; set; }
    public Guid SeasonId { get; set; }
    public Guid PlayerId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? TryoutId { get; set; }
    public long Revision { get; set; }
}
