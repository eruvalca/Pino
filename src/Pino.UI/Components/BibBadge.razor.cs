using Microsoft.AspNetCore.Components;

namespace Pino.UI.Components;

public sealed partial class BibBadge
{
    [Parameter, EditorRequired] public string Number { get; set; } = "";
    [Parameter] public bool Compact { get; set; }
}
