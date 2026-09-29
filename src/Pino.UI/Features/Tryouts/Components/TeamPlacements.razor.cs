using Microsoft.AspNetCore.Components;
using Pino.UI.Features.Tryouts.Models;

namespace Pino.UI.Features.Tryouts.Components;

public sealed partial class TeamPlacements
{
    [CascadingParameter] private SampleTryoutSession Session { get; set; } = null!;
    [Parameter, EditorRequired] public EventCallback<int> OnOpenPlayer { get; set; }
}
