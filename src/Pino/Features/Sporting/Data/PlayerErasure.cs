namespace Pino.Features.Sporting.Data;

// No player identifier, name, free text or content is retained in this receipt.
// The random photo key is transient and cleared when durable cleanup completes.
internal sealed class PlayerErasure
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string ActorId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string? PhotoKey { get; set; }
}
