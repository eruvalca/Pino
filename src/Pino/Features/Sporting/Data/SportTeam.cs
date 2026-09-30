namespace Pino.Features.Sporting.Data;

internal sealed class SportTeam
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid SeasonId { get; set; }
    public string Name { get; set; } = "";
    public int GraduationYear { get; set; }
    public bool Archived { get; set; }
    public long Revision { get; set; }
}
