using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class DecisionHistory
{
    [Parameter, EditorRequired] public IReadOnlyList<DecisionSummary> History { get; set; } = [];
}
