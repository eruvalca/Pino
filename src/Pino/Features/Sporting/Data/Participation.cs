using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Data;

internal sealed class Participation
{
    public Guid ClubId { get; set; }
    public Guid TryoutId { get; set; }
    public Guid PlayerId { get; set; }
    public string Bib { get; set; } = "";
    public DecisionKind Decision { get; set; }
    public Guid? TeamId { get; set; }
    public long Revision { get; set; }
}
