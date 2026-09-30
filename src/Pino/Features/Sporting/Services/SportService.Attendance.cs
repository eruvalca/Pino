using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Features.Sporting.Models;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal async Task<IReadOnlyList<AttendanceSummary>> GetAttendanceAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireMemberAsync(db, actor, clubId, ct);
        if (!await db.TryoutEvents.AnyAsync(value => value.ClubId == clubId && value.Id == tryoutId, ct)) { throw new KeyNotFoundException(); }
        return await db.TryoutAttendances.AsNoTracking().Where(value => value.ClubId == clubId && value.TryoutId == tryoutId &&
                db.Participations.Any(entry => entry.ClubId == clubId && entry.TryoutId == tryoutId && entry.PlayerId == value.PlayerId && !entry.Removed))
            .Select(value => new AttendanceSummary(value.PlayerId, value.Kind, value.Revision, value.RecordedBy, value.RecordedAt, StaffPhoto(value.RecordedById))).ToListAsync(ct);
    }

    internal Task<SportOutcome> SaveAttendanceAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, AttendanceInput input, CancellationToken ct) =>
        WriteAsync<SportOutcome>(actor, clubId, async (db, actorId, token) =>
        {
            if (!Enum.IsDefined(input.Kind)) { return new SportOutcome.Invalid("Choose Not recorded, Present or Absent."); }
            var tryout = await TryoutAsync(db, clubId, tryoutId, token);
            if (tryout is null || await SeasonAsync(db, clubId, tryout.SeasonId, token) is not { Archived: false }) { return new SportOutcome.Invalid("Reopen the season before changing attendance."); }
            if (tryout.Closed) { return new SportOutcome.Invalid("Reopen the tryout before changing attendance."); }
            if (!await db.Participations.AnyAsync(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.PlayerId == input.PlayerId && !value.Removed, token))
            {
                return new SportOutcome.Invalid("Choose a player in this tryout.");
            }
            var attendance = await db.TryoutAttendances.SingleOrDefaultAsync(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.PlayerId == input.PlayerId, token);
            if (attendance?.Kind == input.Kind) { return new SportOutcome.Saved("Attendance is already saved.", input.PlayerId); }
            if ((attendance?.Revision ?? 0) != input.Revision) { return new SportOutcome.Conflict("Attendance changed since you opened it. Reload the page before saving again."); }
            if (attendance is null)
            {
                attendance = new() { ClubId = clubId, TryoutId = tryoutId, PlayerId = input.PlayerId };
                db.TryoutAttendances.Add(attendance);
            }
            attendance.Kind = input.Kind;
            attendance.Revision++;
            attendance.RecordedById = actorId;
            attendance.RecordedBy = await AuthorAsync(db, actorId, token);
            attendance.RecordedAt = time.GetUtcNow();
            tryout.Revision++;
            return new SportOutcome.Saved("Attendance saved. The player's decision has not changed.", input.PlayerId);
        }, ct);
}
