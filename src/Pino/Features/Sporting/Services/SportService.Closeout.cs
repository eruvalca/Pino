using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Pino.Data;
using Pino.Features.Sporting.Data;
using Pino.Features.Sporting.Models;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal Task<TryoutReview> GetTryoutReviewAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, CancellationToken ct) =>
        ReadSnapshotAsync(actor, clubId, (db, token) => ReadTryoutReviewAsync(db, clubId, tryoutId, token), ct);

    private static async Task<TryoutReview> ReadTryoutReviewAsync(ApplicationDbContext db, Guid clubId, Guid tryoutId, CancellationToken ct)
    {
        var current = await ReadCurrentTryoutReviewAsync(db, clubId, tryoutId, ct);
        var closeouts = await db.TryoutCloseouts.AsNoTracking().Include(value => value.Players)
            .Where(value => value.ClubId == clubId && value.TryoutId == tryoutId)
            .OrderByDescending(value => value.ClosedAt).ThenByDescending(value => value.Id).ToListAsync(ct);
        return new(current.Tryout, current.Season, current.Results, closeouts.Select(value => new TryoutCloseoutSummary(value.Id,
            value.TryoutName, value.SeasonName, value.TryoutDate, value.ClosedAt, value.ClosedBy, value.ReopenedAt,
            value.ReopenedBy, value.ReopenReason, value.Players.OrderBy(player => player.LastName, StringComparer.Ordinal)
                .ThenBy(player => player.FirstName, StringComparer.Ordinal).ThenBy(player => player.PlayerId)
                .Select(player => new TryoutResult(player.PlayerId, player.FirstName, player.LastName, player.GraduationYear,
                    player.Bib, player.Decision, player.TeamId, player.TeamName)).ToArray())).ToArray(), current.ReviewToken);
    }

    // Closing needs only the current roster and review token. Historical editions
    // belong to the review read and must not extend the shared write-lock duration.
    private static async Task<CurrentTryoutReview> ReadCurrentTryoutReviewAsync(ApplicationDbContext db, Guid clubId, Guid tryoutId, CancellationToken ct)
    {
        var tryout = await TryoutAsync(db, clubId, tryoutId, ct) ?? throw new KeyNotFoundException();
        var season = await SeasonAsync(db, clubId, tryout.SeasonId, ct) ?? throw new KeyNotFoundException();
        var results = await (from entry in db.Participations
                             join player in db.Players on entry.PlayerId equals player.Id
                             join team in db.SportTeams on entry.TeamId equals team.Id into teams
                             from team in teams.DefaultIfEmpty()
                             where entry.ClubId == clubId && entry.TryoutId == tryoutId
                             orderby player.LastName, player.FirstName, player.Id
                             select new TryoutResult(player.Id, player.FirstName, player.LastName, player.GraduationYear,
                                 entry.Bib, entry.Decision, entry.TeamId, team == null ? null : team.Name)).ToListAsync(ct);
        var summary = new TryoutSummary(tryout.Id, tryout.SeasonId, tryout.Name, tryout.Date, tryout.Location,
            results.Count, results.Count(value => value.Decision != DecisionKind.Awaiting), tryout.Revision, tryout.Closed);
        var seasonSummary = Summary(season);
        // Include displayed catalog/team values as well as the write revision: a review
        // must never close a result whose name, bib or outcome changed after it was read.
        var reviewToken = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { Tryout = summary, Season = seasonSummary, Results = results })));
        return new(summary, seasonSummary, results, reviewToken);
    }

    internal Task<SportOutcome> CloseTryoutAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, CloseTryoutInput input, CancellationToken ct) =>
        WriteAsync<SportOutcome>(actor, clubId, async (db, actorId, token) =>
        {
            if (input.OperationId == Guid.Empty || string.IsNullOrWhiteSpace(input.ReviewToken))
            {
                return new SportOutcome.Invalid("Review the results before closing this tryout.");
            }
            var replay = await db.TryoutCloseouts.SingleOrDefaultAsync(value => value.Id == input.OperationId, token);
            if (replay is not null)
            {
                return replay.ClubId == clubId && replay.TryoutId == tryoutId
                    ? new SportOutcome.Saved("This closeout was already recorded. The latest state is shown.", replay.Id)
                    : new SportOutcome.Conflict(StaleMessage);
            }
            var review = await ReadCurrentTryoutReviewAsync(db, clubId, tryoutId, token);
            if (review.Season.Archived) { return new SportOutcome.Invalid("Reopen the season before closing a tryout."); }
            if (review.Tryout.Closed) { return new SportOutcome.Conflict("This tryout is already closed. Reload to see its recorded results."); }
            if (!string.Equals(review.ReviewToken, input.ReviewToken, StringComparison.Ordinal))
            {
                return new SportOutcome.Conflict("The results changed after your review. Reload and review them before closing.");
            }
            if (!review.Tryout.Complete) { return new SportOutcome.Invalid("Add players and record every decision before closing this tryout."); }
            var tryout = await TryoutAsync(db, clubId, tryoutId, token) ?? throw new KeyNotFoundException();
            tryout.Closed = true;
            tryout.Revision++;
            db.TryoutCloseouts.Add(new()
            {
                Id = input.OperationId,
                ClubId = clubId,
                TryoutId = tryoutId,
                TryoutName = review.Tryout.Name,
                SeasonName = review.Season.Name,
                TryoutDate = review.Tryout.Date,
                ClosedAt = time.GetUtcNow(),
                ClosedBy = await AuthorAsync(db, actorId, token),
                Players = review.Results.Select(value => new TryoutCloseoutPlayer
                {
                    CloseoutId = input.OperationId,
                    PlayerId = value.PlayerId,
                    FirstName = value.FirstName,
                    LastName = value.LastName,
                    GraduationYear = value.GraduationYear,
                    Bib = value.Bib,
                    Decision = value.Decision,
                    TeamId = value.TeamId,
                    TeamName = value.TeamName,
                }).ToList(),
            });
            return new SportOutcome.Saved("Tryout closed. Its reviewed results are preserved.", input.OperationId);
        }, ct);

    internal Task<SportOutcome> ReopenTryoutAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, ReopenTryoutInput input, CancellationToken ct) =>
        WriteAsync<SportOutcome>(actor, clubId, async (db, actorId, token) =>
        {
            var reason = input.Reason?.Trim() ?? "";
            if (reason.Length is < 1 or > 1000) { return new SportOutcome.Invalid("Explain why this tryout is being reopened in 1 to 1,000 characters."); }
            var tryout = await TryoutAsync(db, clubId, tryoutId, token) ?? throw new KeyNotFoundException();
            if (await SeasonAsync(db, clubId, tryout.SeasonId, token) is not { Archived: false })
            {
                return new SportOutcome.Invalid("Reopen the season before reopening its tryout.");
            }
            var closeout = await db.TryoutCloseouts.SingleOrDefaultAsync(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.Id == input.CloseoutId, token);
            if (closeout is null) { return new SportOutcome.Conflict(StaleMessage); }
            if (!tryout.Closed && closeout.ReopenedAt.HasValue && string.Equals(closeout.ReopenReason, reason, StringComparison.Ordinal))
            {
                return new SportOutcome.Saved("This reopening was already recorded. The latest state is shown.", tryoutId);
            }
            if (!tryout.Closed || closeout.ReopenedAt.HasValue) { return new SportOutcome.Conflict("The tryout state changed. Reload before reopening it."); }
            closeout.ReopenedAt = time.GetUtcNow();
            closeout.ReopenedBy = await AuthorAsync(db, actorId, token);
            closeout.ReopenReason = reason;
            tryout.Closed = false;
            tryout.Revision++;
            return new SportOutcome.Saved("Tryout reopened. Earlier closeout editions remain available.", tryoutId);
        }, ct);

    private sealed record CurrentTryoutReview(TryoutSummary Tryout, SeasonSummary Season, IReadOnlyList<TryoutResult> Results, string ReviewToken);
}
