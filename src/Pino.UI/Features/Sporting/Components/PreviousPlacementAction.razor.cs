using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class PreviousPlacementAction
{
    [Parameter, EditorRequired] public RosterEntry Entry { get; set; } = default!;
    [Parameter] public TeamSummary? Team { get; set; }
    [Parameter, EditorRequired] public EventCallback Place { get; set; }
    [Parameter] public bool Disabled { get; set; }
    private bool Available => Team is { Archived: false, Excluded: false } && Entry.Player.GraduationYear >= Team.GraduationYear;
    private bool AlreadyPlaced => Entry.Decision == DecisionKind.Placed && Entry.DecisionTeamId == Entry.PreviousPlacement?.TeamId && Entry.CurrentTeamId == Entry.PreviousPlacement?.TeamId;
    private string UnavailableReason => Team switch
    {
        null or { Archived: true } => "This club team is archived or unavailable. Choose another team in the decision form.",
        { Excluded: true } => "This team is excluded from this season or tryout.",
        _ => "This player's graduation year is not eligible for this team."
    };
}
