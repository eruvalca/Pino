namespace Pino.Features.Sporting.Data;

// Retry acknowledgements survive erasure without retaining player identities or payloads.
internal sealed class SportingBatchReceipt
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid TargetId { get; set; }
    public string Action { get; set; } = "";
    public string ActorId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public int Applied { get; set; }
    public int Skipped { get; set; }
}
