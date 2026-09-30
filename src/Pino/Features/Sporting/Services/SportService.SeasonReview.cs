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
        var teams = await db.SportTeams.AsNoTracking().Where(value => value.ClubId == clubId && value.SeasonId == seasonId).OrderBy(value => value.Name).ToListAsync(ct);
        var players = await (from placement in db.SeasonPlacements
                             join player in db.Players on placement.PlayerId equals player.Id
                             where placement.ClubId == clubId && placement.SeasonId == seasonId
                             orderby player.LastName, player.FirstName, player.Id
                             select new { Player = player, placement.TeamId }).AsNoTracking().ToListAsync(ct);
        var tryouts = await db.TryoutEvents.AsNoTracking().Where(value => value.ClubId == clubId && value.SeasonId == seasonId)
            .OrderByDescending(value => value.Date).ThenBy(value => value.Name)
            .Select(value => new TryoutSummary(value.Id, value.SeasonId, value.Name, value.Date, value.Location,
                db.Participations.Count(entry => entry.ClubId == clubId && entry.TryoutId == value.Id),
                db.Participations.Count(entry => entry.ClubId == clubId && entry.TryoutId == value.Id && entry.Decision != DecisionKind.Awaiting), value.Revision, value.Closed)).ToListAsync(ct);
        return new(Summary(season), teams.Select(team => new SeasonTeamRoster(Summary(team),
            players.Where(value => value.TeamId == team.Id).Select(value => Summary(value.Player)).ToArray())).ToArray(),
            tryouts, players.Where(value => value.TeamId is null).Select(value => Summary(value.Player)).ToArray(), players.Count);
    }
}
