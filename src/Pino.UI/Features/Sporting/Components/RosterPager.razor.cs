using Microsoft.AspNetCore.Components;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class RosterPager
{
    [Parameter, EditorRequired] public int Count { get; set; }
    [Parameter, EditorRequired] public int Page { get; set; }
    [Parameter, EditorRequired] public EventCallback<int> PageChanged { get; set; }
    [Parameter, EditorRequired] public string Label { get; set; } = "";
    [Parameter] public int PageSize { get; set; } = 50;
    [Parameter] public bool Disabled { get; set; }
}
