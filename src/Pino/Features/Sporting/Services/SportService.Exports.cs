using System.Globalization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal Task<byte[]> ExportTeamRosterAsync(ClaimsPrincipal actor, Guid clubId, Guid teamId, Guid seasonId, CancellationToken ct) =>
        ReadSnapshotAsync(actor, clubId, async (db, token) =>
        {
            if (!await db.SportTeams.AnyAsync(value => value.ClubId == clubId && value.Id == teamId, token)) { throw new KeyNotFoundException(); }
            var review = await ReadSeasonReviewAsync(db, clubId, seasonId, token);
            var roster = review.Teams.Single(value => value.Team.Id == teamId);
            return RosterCsv(review.Season, [roster], [], time.GetUtcNow());
        }, ct);

    internal Task<byte[]> ExportSeasonRosterAsync(ClaimsPrincipal actor, Guid clubId, Guid seasonId, CancellationToken ct) =>
        ReadSnapshotAsync(actor, clubId, async (db, token) =>
        {
            var review = await ReadSeasonReviewAsync(db, clubId, seasonId, token);
            return RosterCsv(review.Season, review.Teams, review.UnplacedPlayers, time.GetUtcNow());
        }, ct);

    private static byte[] RosterCsv(SeasonSummary season, IReadOnlyList<SeasonTeamRoster> teams, IReadOnlyList<PlayerSummary> unplaced, DateTimeOffset exportedAt)
    {
        var players = teams.SelectMany(team => team.Players.Select(player => (Player: player, Team: team.Team.Name)))
            .Concat(unplaced.Select(player => (Player: player, Team: "")));
        return SportCsv.Write(["RecordType", "ExportedAtUtc", "Season", "CurrentTeam", "PlayerReference", "FirstName", "MiddleName", "LastName", "GraduationYear", "PrimaryPosition", "SecondaryPosition", "CatalogStatus"],
            players.Select(value => new[] { "Current season roster", exportedAt.ToString("O", CultureInfo.InvariantCulture), season.Name, value.Team, value.Player.PlayerReference,
                value.Player.FirstName, value.Player.MiddleName, value.Player.LastName, value.Player.GraduationYear.ToString(CultureInfo.InvariantCulture), value.Player.Position,
                value.Player.SecondaryPosition, value.Player.Archived ? "Archived" : "Active" }));
    }

    internal Task<byte[]> ExportTryoutResultsAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, Guid? editionId, CancellationToken ct) =>
        ReadSnapshotAsync(actor, clubId, async (db, token) =>
        {
            string tryoutName;
            string seasonName;
            var recordedAt = "";
            IReadOnlyList<TryoutResult> results;
            if (editionId is { } id)
            {
                var edition = await db.TryoutCloseouts.AsNoTracking().Include(value => value.Players)
                    .SingleOrDefaultAsync(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.Id == id, token) ?? throw new KeyNotFoundException();
                tryoutName = edition.TryoutName;
                seasonName = edition.SeasonName;
                recordedAt = edition.ClosedAt.ToString("O", CultureInfo.InvariantCulture);
                results = edition.Players.OrderBy(value => value.LastName, StringComparer.Ordinal).ThenBy(value => value.FirstName, StringComparer.Ordinal).ThenBy(value => value.PlayerId)
                    .Select(value => new TryoutResult(value.PlayerId, value.FirstName, value.LastName, value.GraduationYear, value.Bib, value.Decision, value.TeamId, value.TeamName, value.MiddleName)).ToArray();
            }
            else
            {
                var review = await ReadCurrentTryoutReviewAsync(db, clubId, tryoutId, token);
                tryoutName = review.Tryout.Name;
                seasonName = review.Season.Name;
                results = review.Results;
            }
            var exportedAt = time.GetUtcNow().ToString("O", CultureInfo.InvariantCulture);
            return SportCsv.Write(["RecordType", "ExportedAtUtc", "Season", "Tryout", "EditionId", "RecordedAtUtc", "FirstName", "MiddleName", "LastName", "GraduationYear", "Bib", "Decision", "DecisionTeam"],
                results.Select(value => new[] { editionId.HasValue ? "Recorded tryout edition" : "Current tryout outcomes", exportedAt, seasonName, tryoutName, editionId?.ToString() ?? "", recordedAt,
                    value.FirstName, value.MiddleName, value.LastName, value.GraduationYear.ToString(CultureInfo.InvariantCulture), value.Bib, ExportDecision(value.Decision), value.TeamName ?? "" }));
        }, ct);

    private static string ExportDecision(DecisionKind kind) => kind switch
    {
        DecisionKind.Awaiting => "Awaiting decision",
        DecisionKind.Placed => "Placed",
        DecisionKind.Withdrawn => "Withdrawn",
        DecisionKind.NotSelected => "Not selected",
        DecisionKind.DidNotAttend => "Did not attend",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
