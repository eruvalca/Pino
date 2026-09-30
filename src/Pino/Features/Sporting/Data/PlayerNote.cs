namespace Pino.Features.Sporting.Data;

internal sealed class PlayerNote
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid TryoutId { get; set; }
    public Guid PlayerId { get; set; }
    public string Text { get; set; } = "";
    public string AuthorId { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CorrectsId { get; set; }
    public Guid? RedactionOperationId { get; set; }
    public DateTimeOffset? RedactedAt { get; set; }
    public string? RedactedById { get; set; }
    public string? RedactedBy { get; set; }
    public string? RedactionReason { get; set; }
}
