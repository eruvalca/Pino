using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Clubs;

namespace Pino.UI.Features.Clubs.Components;

public sealed partial class ClubPageShell
{
    [Parameter] public ClubSummary? Club { get; set; }
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;
}
