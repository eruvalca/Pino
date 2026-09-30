namespace Pino.Features.Sporting.Data;

internal sealed class EnrollmentChange
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid TryoutId { get; set; }
    public Guid PlayerId { get; set; }
    public bool Removed { get; set; }
    public string Reason { get; set; } = "";
    public string AuthorId { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string Bib { get; set; } = "";
}
