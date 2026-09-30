using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class TryoutCreationReview
{
    [Parameter, EditorRequired] public TryoutInput Input { get; set; } = default!;
    [Parameter, EditorRequired] public int ActivePlayers { get; set; }
    [Parameter, EditorRequired] public IReadOnlyList<TeamSummary> Teams { get; set; } = [];
    [Parameter, EditorRequired] public EventCallback Confirm { get; set; }
    [Parameter, EditorRequired] public EventCallback Back { get; set; }
    [Parameter, EditorRequired] public EventCallback Cancel { get; set; }
    [Parameter] public bool Disabled { get; set; }
    private IReadOnlyList<TeamSummary> IncludedTeams { get; set; } = [];
    protected override void OnParametersSet() => IncludedTeams = Teams.Where(team => !team.Archived && !team.Excluded).ToArray();
}
