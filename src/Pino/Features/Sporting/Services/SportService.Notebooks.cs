using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal async Task<PlayerNotebook> GetNotebookAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, Guid playerId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var actorId = await RequireMemberAsync(db, actor, clubId, ct);
        var entry = await db.Participations.AsNoTracking().SingleOrDefaultAsync(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.PlayerId == playerId, ct)
            ?? throw new KeyNotFoundException();
        var notes = await db.PlayerNotes.AsNoTracking().Where(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.PlayerId == playerId)
            .OrderByDescending(value => value.CreatedAt).ThenByDescending(value => value.Id).ToListAsync(ct);
        var corrected = notes.Where(note => note.CorrectsId.HasValue).Select(note => note.CorrectsId!.Value).ToHashSet();
        var history = await db.DecisionEvents.AsNoTracking().Where(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.PlayerId == playerId)
            .OrderByDescending(value => value.CreatedAt).ThenByDescending(value => value.Id).ToListAsync(ct);
        var enrollmentChanges = await db.EnrollmentChanges.AsNoTracking().Where(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.PlayerId == playerId)
            .OrderByDescending(value => value.CreatedAt).ThenByDescending(value => value.Id)
            .Select(value => new EnrollmentChangeSummary(value.Id, value.Removed, value.Reason, value.Author, value.CreatedAt, value.Bib, StaffPhoto(value.AuthorId))).ToListAsync(ct);
        return new(playerId, entry.Removed, notes.Select(note => new NoteSummary(note.Id, note.PlayerId, note.Text, note.Author, note.CreatedAt, note.CorrectsId,
            !entry.Removed && note.RedactedAt is null && string.Equals(note.AuthorId, actorId, StringComparison.Ordinal) && !corrected.Contains(note.Id),
            note.RedactedAt, note.RedactedBy, note.RedactionReason, StaffPhoto(note.AuthorId), StaffPhoto(note.RedactedById))).ToArray(), history.Select(Summary).ToArray(), enrollmentChanges);
    }
}
