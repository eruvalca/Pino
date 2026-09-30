namespace Pino.Features.Sporting.Data;

internal sealed class Season
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string Name { get; set; } = "";
    public DateOnly StartsOn { get; set; }
    public DateOnly EndsOn { get; set; }
    public bool Archived { get; set; }
    public long Revision { get; set; }
}
