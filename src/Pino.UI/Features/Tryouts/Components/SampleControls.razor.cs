using Microsoft.AspNetCore.Components;
using Pino.UI.Features.Tryouts.Models;

namespace Pino.UI.Features.Tryouts.Components;

public sealed partial class SampleControls
{
    private string _roster = "standard";
    private bool _loading;
    [CascadingParameter] private SampleTryoutSession Session { get; set; } = null!;
    [Parameter, EditorRequired] public EventCallback<string> OnReload { get; set; }
    [Parameter, EditorRequired] public EventCallback OnChanged { get; set; }
    private Task NotifyAsync() => OnChanged.InvokeAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        await OnReload.InvokeAsync(_roster);
        _loading = false;
    }
}
