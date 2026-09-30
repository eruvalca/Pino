namespace Pino.Features.Sporting.Data;

internal sealed class Player
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string PlayerReference { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int GraduationYear { get; set; }
    public string Position { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string? PhotoKey { get; set; }
    public bool Archived { get; set; }
    public long Revision { get; set; }
}
