using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    private static readonly JsonSerializerOptions _personalExportOptions = new() { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    internal async Task<byte[]> ExportPlayerAsync(ClaimsPrincipal actor, Guid clubId, Guid playerId, CancellationToken ct)
    {
        var snapshot = await ReadSnapshotAsync(actor, clubId, async (db, token) =>
        {
            await RequireAdministratorAsync(db, actor.FindFirstValue(ClaimTypes.NameIdentifier)!, clubId, token);
            var player = await db.Players.AsNoTracking().SingleOrDefaultAsync(value => value.ClubId == clubId && value.Id == playerId, token) ?? throw new KeyNotFoundException();
            var notes = await (from note in db.PlayerNotes
                               join tryout in db.TryoutEvents on note.TryoutId equals tryout.Id
                               where note.ClubId == clubId && note.PlayerId == playerId
                               orderby note.CreatedAt, note.Id
                               select new { Note = note, Tryout = new { tryout.Name, tryout.Date } }).AsNoTracking().ToArrayAsync(token);
            var decisions = await db.DecisionEvents.AsNoTracking().Where(value => value.ClubId == clubId && value.PlayerId == playerId).OrderBy(value => value.CreatedAt).ThenBy(value => value.Id).ToArrayAsync(token);
            var placements = await (from placement in db.SeasonPlacements
                                    join season in db.Seasons on placement.SeasonId equals season.Id
                                    join team in db.SportTeams on placement.TeamId equals team.Id into teams
                                    from team in teams.DefaultIfEmpty()
                                    where placement.ClubId == clubId && placement.PlayerId == playerId
                                    orderby season.StartsOn, season.Id
                                    select new { Placement = placement, Season = season.Name, Team = team == null ? null : team.Name }).AsNoTracking().ToArrayAsync(token);
            var participation = await (from entry in db.Participations
                                       join tryout in db.TryoutEvents on entry.TryoutId equals tryout.Id
                                       join season in db.Seasons on tryout.SeasonId equals season.Id
                                       where entry.ClubId == clubId && entry.PlayerId == playerId
                                       orderby tryout.Date, tryout.Id
                                       select new { Enrollment = entry, Tryout = new { tryout.Name, tryout.Date, tryout.Location, tryout.Closed }, Season = season.Name }).AsNoTracking().ToArrayAsync(token);
            var enrollmentChanges = await db.EnrollmentChanges.AsNoTracking().Where(value => value.ClubId == clubId && value.PlayerId == playerId).OrderBy(value => value.CreatedAt).ThenBy(value => value.Id).ToArrayAsync(token);
            var editions = await (from result in db.TryoutCloseoutPlayers
                                  join edition in db.TryoutCloseouts on result.CloseoutId equals edition.Id
                                  where edition.ClubId == clubId && result.PlayerId == playerId
                                  orderby edition.ClosedAt, edition.Id
                                  select new { Result = result, Edition = new { edition.Id, edition.TryoutId, edition.TryoutName, edition.TryoutDate, edition.SeasonName, edition.ClosedAt, edition.ClosedBy, edition.ReopenedAt, edition.ReopenedBy, edition.ReopenReason } }).AsNoTracking().ToArrayAsync(token);
            return new
            {
                player.PhotoKey,
                Content = new
                {
                    ExportedAtUtc = time.GetUtcNow(),
                    ClubId = clubId,
                    Player = new { player.Id, player.PlayerReference, player.FirstName, player.MiddleName, player.LastName, player.GraduationYear, player.Position, player.SecondaryPosition, player.ContactEmail, player.Archived, player.Revision },
                    Notes = notes,
                    Decisions = decisions,
                    Placements = placements,
                    Participation = participation,
                    EnrollmentChanges = enrollmentChanges,
                    RecordedEditions = editions,
                },
            };
        }, ct);
        // Download after the database snapshot. A missing/unavailable saved photo fails the
        // export instead of silently presenting an incomplete package as comprehensive.
        var photo = snapshot.PhotoKey is { } key ? Convert.ToBase64String(await photos.DownloadAsync(key, ct)) : null;
        return JsonSerializer.SerializeToUtf8Bytes(new { Data = snapshot.Content, PhotoJpegBase64 = photo }, _personalExportOptions);
    }
}
