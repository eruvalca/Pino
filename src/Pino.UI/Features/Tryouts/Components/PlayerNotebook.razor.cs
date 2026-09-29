using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Pino.UI.Features.Tryouts.Models;

namespace Pino.UI.Features.Tryouts.Components;

public sealed partial class PlayerNotebook
{
    private ElementReference _heading;
    private int _focusedRequest;
    [CascadingParameter] private SampleTryoutSession Session { get; set; } = null!;
    [Parameter, EditorRequired] public EventCallback OnBack { get; set; }
    [Parameter, EditorRequired] public EventCallback OnChanged { get; set; }
    [Parameter] public int FocusRequest { get; set; }
    private SamplePlayer Player => Session.Selected!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusedRequest != FocusRequest)
        {
            _focusedRequest = FocusRequest;
            await _heading.FocusAsync();
        }
    }
}
