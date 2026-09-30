namespace Pino.Features.Sporting.Data;

internal sealed class TryoutEvent
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid SeasonId { get; set; }
    public string Name { get; set; } = "";
    public DateOnly Date { get; set; }
    public string Location { get; set; } = "";
    public long Revision { get; set; }
}
