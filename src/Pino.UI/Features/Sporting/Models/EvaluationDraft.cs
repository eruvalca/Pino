using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Models;

internal sealed class EvaluationDraft(RosterEntry entry)
{
    internal string Note { get; set; } = "";
    internal Guid NoteId { get; set; } = Guid.NewGuid();
    internal Guid? CorrectsId { get; set; }
    internal NoteInput? LastNoteAttempt { get; set; }
    internal DecisionInput? LastDecisionAttempt { get; set; }
    internal DecisionKind Kind { get; set; } = entry.Decision;
    internal Guid? TeamId { get; set; } = entry.DecisionTeamId;
    internal string Reason { get; set; } = "";
    internal long Revision { get; private set; } = entry.Revision;
    internal long PlacementRevision { get; private set; } = entry.PlacementRevision;
    internal Guid OperationId { get; private set; } = Guid.NewGuid();
    internal void ReloadDecision(RosterEntry current)
    {
        Kind = current.Decision;
        TeamId = current.DecisionTeamId;
        Reason = "";
        RebaseDecision(current);
    }

    internal void RebaseDecision(RosterEntry current)
    {
        Revision = current.Revision;
        PlacementRevision = current.PlacementRevision;
        OperationId = Guid.NewGuid();
        LastDecisionAttempt = null;
    }
}
