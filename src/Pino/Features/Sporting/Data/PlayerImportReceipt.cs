namespace Pino.Features.Sporting.Data;

// A retry receipt contains counts only, never the CSV or player identities.
internal sealed class PlayerImportReceipt
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string ActorId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public int Created { get; set; }
    public int Skipped { get; set; }
    public int Reactivated { get; set; }
}
