using Pino.Features.Sporting.Data;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal static class NoteRedaction
{
    internal static void Apply(IReadOnlyList<PlayerNote> notes, PlayerNote selected, RedactNoteInput input,
        string actorId, string actorName, DateTimeOffset now)
    {
        var related = notes.Where(note => note.ClubId == selected.ClubId && note.TryoutId == selected.TryoutId && note.PlayerId == selected.PlayerId).ToArray();
        var chain = new HashSet<Guid> { selected.Id };
        bool changed;
        do
        {
            changed = false;
            foreach (var note in related)
            {
                if (note.CorrectsId is not { } parent) { continue; }
                if (chain.Contains(note.Id)) { changed |= chain.Add(parent); }
                if (chain.Contains(parent)) { changed |= chain.Add(note.Id); }
            }
        }
        while (changed);

        foreach (var note in related.Where(note => chain.Contains(note.Id)))
        {
            note.Text = "";
            note.RedactionOperationId = input.OperationId;
            note.RedactedAt = now;
            note.RedactedById = actorId;
            note.RedactedBy = actorName;
            note.RedactionReason = input.Reason.Trim();
        }
    }
}
