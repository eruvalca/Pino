using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal Task<SeasonReview> GetSeasonReviewAsync(ClaimsPrincipal actor, Guid clubId, Guid seasonId, CancellationToken ct) =>
        ReadSnapshotAsync(actor, clubId, (db, token) => ReadSeasonReviewAsync(db, clubId, seasonId, token), ct);

    private static async Task<SeasonReview> ReadSeasonReviewAsync(Pino.Data.ApplicationDbContext db, Guid clubId, Guid seasonId, CancellationToken ct)
    {
        var season = await SeasonAsync(db, clubId, seasonId, ct) ?? throw new KeyNotFoundException();
        var teams = await ReadTeamsAsync(db, clubId, seasonId, tryoutId: null, ct);
        var players = await (from placement in db.SeasonPlacements
                             join player in db.Players on placement.PlayerId equals player.Id
                             where placement.ClubId == clubId && placement.SeasonId == seasonId && (placement.TeamId != null ||
                                 db.Participations.Any(entry => entry.ClubId == clubId && entry.PlayerId == placement.PlayerId && !entry.Removed &&
                                     db.TryoutEvents.Any(tryout => tryout.ClubId == clubId && tryout.Id == entry.TryoutId && tryout.SeasonId == seasonId)))
                             orderby player.LastName, player.FirstName, player.Id
                             select new { Player = player, placement.TeamId }).AsNoTracking().ToListAsync(ct);
        var tryouts = await db.TryoutEvents.AsNoTracking().Where(value => value.ClubId == clubId && value.SeasonId == seasonId)
            .OrderByDescending(value => value.Date).ThenBy(value => value.Name)
            .Select(value => new TryoutSummary(value.Id, value.SeasonId, value.Name, value.Date, value.Location,
                db.Participations.Count(entry => entry.ClubId == clubId && entry.TryoutId == value.Id && !entry.Removed),
                db.Participations.Count(entry => entry.ClubId == clubId && entry.TryoutId == value.Id && !entry.Removed && entry.Decision != DecisionKind.Awaiting), value.Revision, value.Closed)).ToListAsync(ct);
        return new(Summary(season), teams.Select(team => new SeasonTeamRoster(team,
            players.Where(value => value.TeamId == team.Id).Select(value => Summary(value.Player)).ToArray())).ToArray(),
            tryouts, players.Where(value => value.TeamId is null).Select(value => Summary(value.Player)).ToArray(), players.Count);
    }
}
