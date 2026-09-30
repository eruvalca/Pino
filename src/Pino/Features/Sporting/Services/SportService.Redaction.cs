using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Features.Sporting.Models;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal Task<SportOutcome> RedactNoteAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, RedactNoteInput input, CancellationToken ct) =>
        WriteAsync<SportOutcome>(actor, clubId, async (db, actorId, token) =>
        {
            await RequireAdministratorAsync(db, actorId, clubId, token);
            if (input.OperationId == Guid.Empty || string.IsNullOrWhiteSpace(input.Reason) || input.Reason.Length > 1000 || !input.ConfirmAllVersions)
            {
                return new SportOutcome.Invalid("Give a reason of 1 to 1,000 characters and confirm removal from every version of this note.");
            }
            var note = await db.PlayerNotes.SingleOrDefaultAsync(value => value.Id == input.NoteId && value.ClubId == clubId && value.TryoutId == tryoutId, token);
            if (note is null) { return new SportOutcome.Invalid("This note is no longer available. Reload the notebook."); }
            if (note.RedactedAt is not null)
            {
                return new SportOutcome.Saved("This note's text and earlier versions have already been removed.", note.Id);
            }
            if (await db.PlayerNotes.AnyAsync(value => value.ClubId == clubId && value.RedactionOperationId == input.OperationId, token))
            {
                return new SportOutcome.Conflict("This request already removed text from another note. Reload before trying again.");
            }
            var versions = await db.PlayerNotes.Where(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.PlayerId == note.PlayerId).ToListAsync(token);
            NoteRedaction.Apply(versions, note, input, actorId, await AuthorAsync(db, actorId, token), time.GetUtcNow());
            var tryout = await TryoutAsync(db, clubId, tryoutId, token);
            if (tryout is not null) { tryout.Revision++; }
            return new SportOutcome.Saved("Note text removed from every version. The reason, administrator and time remain in history.", note.Id);
        }, ct);
}
