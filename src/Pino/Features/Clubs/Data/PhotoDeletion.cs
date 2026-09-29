namespace Pino.Features.Clubs.Data;

internal sealed class PhotoDeletion
{
    public string PhotoKey { get; set; } = "";
    public DateTimeOffset NotBefore { get; set; }
}
