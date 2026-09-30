using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Clubs;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class SportFrame
{
    [Parameter] public MembershipSummary? Membership { get; set; }
    [Parameter] public bool Busy { get; set; }
    [Parameter] public bool Failed { get; set; }
    [Parameter] public string? Message { get; set; }
    [Parameter] public string MessageKind { get; set; } = "information";
    [Parameter, EditorRequired] public EventCallback Reload { get; set; }
    [Parameter, EditorRequired] public RenderFragment ChildContent { get; set; } = default!;
}
