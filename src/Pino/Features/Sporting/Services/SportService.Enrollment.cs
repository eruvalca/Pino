using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Data;
using Pino.Features.Sporting.Data;
using Pino.Features.Sporting.Models;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal async Task<IReadOnlyList<EnrollmentDetail>> GetEnrollmentsAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, CancellationToken ct)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await RequireMemberAsync(db, actor, clubId, ct);
        var tryout = await TryoutAsync(db, clubId, tryoutId, ct) ?? throw new KeyNotFoundException();
        var entries = await (from entry in db.Participations
                             join player in db.Players on entry.PlayerId equals player.Id
                             join placement in db.SeasonPlacements on new { entry.ClubId, SeasonId = tryout.SeasonId, entry.PlayerId } equals new { placement.ClubId, placement.SeasonId, placement.PlayerId }
                             where entry.ClubId == clubId && entry.TryoutId == tryoutId
                             orderby player.LastName, player.FirstName, player.Id
                             select new { Entry = entry, Player = player, Placement = placement }).AsNoTracking().ToListAsync(ct);
        var changes = await db.EnrollmentChanges.AsNoTracking().Where(value => value.ClubId == clubId && value.TryoutId == tryoutId)
            .OrderByDescending(value => value.CreatedAt).ThenByDescending(value => value.Id).ToListAsync(ct);
        var history = changes.ToLookup(value => value.PlayerId);
        return entries.Select(value => new EnrollmentDetail(new(Summary(value.Player), value.Entry.Bib, value.Entry.Decision, value.Entry.TeamId,
            value.Entry.Revision, value.Placement.TeamId, value.Placement.TryoutId, value.Placement.Revision), value.Entry.Removed,
            history[value.Player.Id].Select(change => new EnrollmentChangeSummary(change.Id, change.Removed, change.Reason, change.Author, change.CreatedAt, change.Bib, StaffPhoto(change.AuthorId))).ToArray())).ToArray();
    }

    internal Task<SportOutcome> ChangeEnrollmentAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, EnrollmentChangeInput input, CancellationToken ct) =>
        WriteAsync(actor, clubId, async (db, actorId, token) =>
        {
            var reason = input.Reason?.Trim() ?? "";
            var bib = input.Bib?.Trim() ?? "";
            if (input.OperationId == Guid.Empty || reason.Length is < 1 or > 1000 || bib.Length > 20)
            {
                return new SportOutcome.Invalid("Give a reason of 1 to 1,000 characters and a bib of up to 20 characters.");
            }
            var replay = await db.EnrollmentChanges.SingleOrDefaultAsync(value => value.Id == input.OperationId, token);
            if (replay is not null) { return ReplayedEnrollment(replay, actorId, clubId, tryoutId, input, reason, bib); }
            var tryout = await TryoutAsync(db, clubId, tryoutId, token);
            if (tryout is null || tryout.Closed || await SeasonAsync(db, clubId, tryout.SeasonId, token) is not { Archived: false })
            {
                return new SportOutcome.Invalid("Reopen the season and tryout before changing the player list.");
            }
            var entry = await db.Participations.SingleOrDefaultAsync(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.PlayerId == input.PlayerId, token);
            if (entry is null) { return new SportOutcome.Invalid("This player is not on this tryout's list."); }
            var placement = await db.SeasonPlacements.SingleAsync(value => value.ClubId == clubId && value.SeasonId == tryout.SeasonId && value.PlayerId == input.PlayerId, token);
            if (entry.Revision != input.Revision || placement.Revision != input.PlacementRevision || entry.Removed == input.Remove)
            {
                return new SportOutcome.Conflict(StaleMessage);
            }
            if (!input.Remove)
            {
                var failure = await ValidateRestorationAsync(db, clubId, tryoutId, input.PlayerId, bib, token);
                if (failure is not null) { return new SportOutcome.Invalid(failure); }
            }
            return ApplyEnrollmentChange(db, actorId, await AuthorAsync(db, actorId, token), tryout, entry, placement, input, reason, bib);
        }, ct);

    private static SportOutcome ReplayedEnrollment(EnrollmentChange replay, string actorId, Guid clubId, Guid tryoutId, EnrollmentChangeInput input, string reason, string bib) =>
        replay.ClubId == clubId && replay.TryoutId == tryoutId && replay.PlayerId == input.PlayerId && replay.Removed == input.Remove &&
        string.Equals(replay.AuthorId, actorId, StringComparison.Ordinal) && string.Equals(replay.Reason, reason, StringComparison.Ordinal) &&
        (input.Remove || string.Equals(replay.Bib, bib, StringComparison.Ordinal))
            ? new SportOutcome.Saved("This roster change was already saved. The latest record is shown.", input.PlayerId)
            : new SportOutcome.Conflict("That roster change was saved with different details. Reload the history before continuing.");

    private static async Task<string?> ValidateRestorationAsync(ApplicationDbContext db, Guid clubId, Guid tryoutId, Guid playerId, string bib, CancellationToken ct)
    {
        if (!await db.Players.AnyAsync(value => value.ClubId == clubId && value.Id == playerId && !value.Archived, ct))
        {
            return "Restore this archived player in Players before adding them back to the tryout.";
        }
        if (bib.Length > 0 && await db.Participations.AnyAsync(value => value.ClubId == clubId && value.TryoutId == tryoutId && !value.Removed && value.Bib == bib, ct))
        {
            return "That bib is now assigned to another player. Choose another or leave it empty.";
        }
        return await db.Participations.CountAsync(value => value.ClubId == clubId && value.TryoutId == tryoutId && !value.Removed, ct) >= 2000
            ? "This tryout has reached 2,000 retained players." : null;
    }

    private SportOutcome ApplyEnrollmentChange(ApplicationDbContext db, string actorId, string author, TryoutEvent tryout, Participation entry,
        SeasonPlacement placement, EnrollmentChangeInput input, string reason, string bib)
    {
        if (input.Remove && placement.TryoutId == tryout.Id)
        {
            placement.TeamId = null;
            placement.TryoutId = null;
            placement.Revision++;
        }
        if (!input.Remove)
        {
            entry.Bib = bib;
            entry.Decision = DecisionKind.Awaiting;
            entry.TeamId = null;
        }
        entry.Removed = input.Remove;
        entry.Revision++;
        tryout.Revision++;
        db.EnrollmentChanges.Add(new()
        {
            Id = input.OperationId,
            ClubId = tryout.ClubId,
            TryoutId = tryout.Id,
            PlayerId = input.PlayerId,
            Removed = input.Remove,
            Reason = reason,
            AuthorId = actorId,
            Author = author,
            CreatedAt = time.GetUtcNow(),
            Bib = entry.Bib,
        });
        return new SportOutcome.Saved(input.Remove
            ? "Player excluded. Earlier work stays in history. This player no longer needs a decision to close the tryout."
            : "Player restored. Earlier work stays in history. Save a new final decision before closing the tryout.", input.PlayerId);
    }
}
