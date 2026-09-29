namespace Pino.Features.Clubs.Data;

internal sealed class ClubProfile
{
    public string UserId { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? PhotoKey { get; set; }
}
