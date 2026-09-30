using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Data;

internal sealed class DecisionEvent
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid PlayerId { get; set; }
    public Guid TryoutId { get; set; }
    public string TryoutName { get; set; } = "";
    public string SeasonName { get; set; } = "";
    public DecisionKind Kind { get; set; }
    public string? TeamName { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? PreviousTeamId { get; set; }
    public string AuthorId { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public string Reason { get; set; } = "";
}
