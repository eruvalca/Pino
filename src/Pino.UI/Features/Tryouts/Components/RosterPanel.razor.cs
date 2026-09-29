using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Pino.UI.Features.Tryouts.Models;

namespace Pino.UI.Features.Tryouts.Components;

public sealed partial class RosterPanel
{
    private ElementReference _searchInput;
    private int _focusedRequest;
    [CascadingParameter] private SampleTryoutSession Session { get; set; } = null!;
    [Parameter, EditorRequired] public EventCallback<int> OnSelected { get; set; }
    [Parameter] public int FocusRequest { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusedRequest != FocusRequest)
        {
            _focusedRequest = FocusRequest;
            await _searchInput.FocusAsync();
        }
    }
}
