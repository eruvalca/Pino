using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Models;

namespace Pino.UI.Features.Sporting.Pages;

public sealed partial class TryoutWork
{
    private async Task<bool> ReconcileNoteAsync(EvaluationDraft draft, NoteInput input)
    {
        if (draft.LastNoteAttempt is null) { return true; }
        var canSubmit = false;
        await ExecuteAsync(async () =>
        {
            await LoadNotebookAsync();
            if (_notebook!.Removed) { Message = "This player was excluded. Open Manage tryout players before continuing."; MessageKind = "warning"; return; }
            var saved = Notes.SingleOrDefault(note => note.Id == input.Id);
            if (saved?.RedactedAt is not null || (input.CorrectsId is { } corrected && Notes.Any(note => note.Id == corrected && note.RedactedAt is not null)))
            {
                Message = "This note's private text was removed. Your unfinished correction was cleared. Nothing was sent.";
                MessageKind = "warning";
                return;
            }
            if (saved is null) { canSubmit = true; return; }
            draft.LastNoteAttempt = null;
            draft.NoteId = Guid.NewGuid();
            if (saved.CorrectsId == input.CorrectsId && string.Equals(saved.Text, input.Text.Trim(), StringComparison.Ordinal))
            {
                draft.Note = "";
                draft.CorrectsId = null;
                Message = "Your earlier note was saved. No duplicate was added.";
                MessageKind = "success";
            }
            else
            {
                draft.CorrectsId = saved.CanCorrect ? saved.Id : null;
                Message = saved.CanCorrect
                    ? "Your earlier note was saved. Your newer edits are still here as a correction; review them and save again."
                    : "Your earlier note was saved and has since been corrected. Your newer edits are still here as a new note; review them and save again.";
                MessageKind = "warning";
            }
        });
        return canSubmit;
    }

    private async Task<bool> ReconcileDecisionAsync(EvaluationDraft draft, DecisionInput input)
    {
        if (draft.LastDecisionAttempt is not { } attempted) { return true; }
        var canSubmit = false;
        await ExecuteAsync(async () =>
        {
            await LoadAsync();
            if (!_data!.Roster.Any(entry => entry.Player.Id == input.PlayerId)) { Message = "This player was excluded. Open Manage tryout players before continuing."; MessageKind = "warning"; return; }
            var saved = History.SingleOrDefault(decision => decision.Id == attempted.OperationId);
            if (saved is null) { canSubmit = true; return; }
            var current = _data.Roster.Single(entry => entry.Player.Id == input.PlayerId);
            if (saved.Kind == input.Kind && saved.TeamId == input.TeamId && string.Equals(saved.Reason, input.Reason.Trim(), StringComparison.Ordinal))
            {
                draft.ReloadDecision(current);
                Message = "Your earlier decision was saved. The latest record is shown.";
                MessageKind = "success";
            }
            else
            {
                draft.RebaseDecision(current);
                Message = "Your earlier decision was saved. Your newer edits are still here; review the current placement and save again.";
                MessageKind = "warning";
            }
        });
        return canSubmit;
    }
}
