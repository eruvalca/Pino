using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Data;

internal sealed class TryoutCloseoutPlayer
{
    public Guid CloseoutId { get; set; }
    public Guid PlayerId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int GraduationYear { get; set; }
    public string Bib { get; set; } = "";
    public DecisionKind Decision { get; set; }
    public Guid? TeamId { get; set; }
    public string? TeamName { get; set; }
}
