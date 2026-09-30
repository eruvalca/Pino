using Microsoft.AspNetCore.Components;

namespace Pino.UI.Components;

public sealed partial class WorkflowProgress
{
    [Parameter, EditorRequired] public IReadOnlyList<string> Steps { get; set; } = [];
    [Parameter, EditorRequired] public int Current { get; set; }
    [Parameter] public string Label { get; set; } = "Progress";
}
