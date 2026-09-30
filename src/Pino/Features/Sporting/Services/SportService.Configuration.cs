using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Features.Sporting.Data;
using Pino.Features.Sporting.Models;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal Task<SportOutcome> SaveSeasonAsync(ClaimsPrincipal actor, Guid clubId, SeasonInput input, CancellationToken ct) =>
        WriteAsync<SportOutcome>(actor, clubId, async (db, _, token) =>
        {
            if (!SportRules.Valid(input) || input.Id == Guid.Empty || input.StartsOn.Year < 2000 || input.EndsOn.Year > 2100 || input.StartsOn > input.EndsOn)
            {
                return new SportOutcome.Invalid("Enter a season name and dates between 2000 and 2100, with the end on or after the start.");
            }
            var season = await SeasonAsync(db, clubId, input.Id, token);
            if ((season?.Revision ?? 0) != input.Revision) { return new SportOutcome.Conflict(StaleMessage); }
            if (await db.Seasons.AnyAsync(value => value.ClubId == clubId && value.Id != input.Id && EF.Functions.ILike(value.Name, SportRules.EscapeLike(input.Name.Trim()), "\\"), token))
            {
                return new SportOutcome.Invalid("A season with this name already exists. Use a distinct name.");
            }
            if (await db.TryoutEvents.AnyAsync(value => value.ClubId == clubId && value.SeasonId == input.Id && (value.Date < input.StartsOn || value.Date > input.EndsOn), token))
            {
                return new SportOutcome.Invalid("Season dates must include all its tryouts. Change the tryout dates first.");
            }
            if (season is null)
            {
                season = new() { Id = input.Id, ClubId = clubId };
                db.Seasons.Add(season);
            }
            season.Name = input.Name.Trim();
            season.StartsOn = input.StartsOn;
            season.EndsOn = input.EndsOn;
            season.Archived = input.Archived;
            season.Revision++;
            return new SportOutcome.Saved(input.Archived ? "Season archived. Its records remain available." : "Season saved.", season.Id);
        }, ct);

    internal Task<SportOutcome> SaveTeamAsync(ClaimsPrincipal actor, Guid clubId, TeamInput input, CancellationToken ct) =>
        WriteAsync<SportOutcome>(actor, clubId, async (db, _, token) =>
        {
            if (!SportRules.Valid(input) || input.Id == Guid.Empty) { return new SportOutcome.Invalid("Enter a team name and graduation year between 2000 and 2100."); }
            var team = await db.SportTeams.SingleOrDefaultAsync(value => value.ClubId == clubId && value.Id == input.Id, token);
            if ((team?.Revision ?? 0) != input.Revision) { return new SportOutcome.Conflict(StaleMessage); }
            if (await db.SportTeams.AnyAsync(value => value.ClubId == clubId && value.Id != input.Id && EF.Functions.ILike(value.Name, SportRules.EscapeLike(input.Name.Trim()), "\\"), token))
            {
                return new SportOutcome.Invalid("This club already has a team with that name.");
            }
            var ineligible = await (from placement in db.SeasonPlacements
                                    join player in db.Players on placement.PlayerId equals player.Id
                                    join season in db.Seasons on placement.SeasonId equals season.Id
                                    where placement.ClubId == clubId && placement.TeamId == input.Id && !season.Archived && (input.Archived || player.GraduationYear < input.GraduationYear)
                                    select player.Id).AnyAsync(token);
            if (ineligible) { return new SportOutcome.Invalid("Move affected players in active seasons before archiving this team or raising its graduation-year requirement. Archived seasons remain unchanged."); }
            if (team is null)
            {
                team = new() { Id = input.Id, ClubId = clubId };
                db.SportTeams.Add(team);
            }
            team.Name = input.Name.Trim();
            team.GraduationYear = input.GraduationYear;
            team.Archived = input.Archived;
            team.Revision++;
            return new SportOutcome.Saved("Team saved.", team.Id);
        }, ct);

    internal Task<SportOutcome> SaveTryoutAsync(ClaimsPrincipal actor, Guid clubId, TryoutInput input, CancellationToken ct) =>
        WriteAsync(actor, clubId, async (db, _, token) =>
        {
            if (!SportRules.Valid(input) || input.Id == Guid.Empty) { return new SportOutcome.Invalid("Enter a tryout name and location of up to 160 characters."); }
            var season = await SeasonAsync(db, clubId, input.SeasonId, token);
            if (season?.Archived != false) { return new SportOutcome.Invalid("Choose an active season. Reopen an archived season to change its tryouts."); }
            if (input.Date < season.StartsOn || input.Date > season.EndsOn) { return new SportOutcome.Invalid("The tryout date must fall within the season."); }
            var tryout = await TryoutAsync(db, clubId, input.Id, token);
            if (tryout?.Closed == true) { return new SportOutcome.Invalid("Reopen the tryout before changing its details."); }
            if ((tryout?.Revision ?? 0) != input.Revision) { return new SportOutcome.Conflict(StaleMessage); }
            if (tryout is not null && tryout.SeasonId != input.SeasonId) { return new SportOutcome.Invalid("A tryout's season cannot change. Create a new tryout in the other season."); }
            if (await db.TryoutEvents.AnyAsync(value => value.ClubId == clubId && value.SeasonId == input.SeasonId && value.Id != input.Id && EF.Functions.ILike(value.Name, SportRules.EscapeLike(input.Name.Trim()), "\\"), token))
            {
                return new SportOutcome.Invalid("This season already has a tryout with that name.");
            }
            return await SaveTryoutDetailsAsync(db, clubId, input, tryout, token);
        }, ct);

    private static async Task<SportOutcome> SaveTryoutDetailsAsync(Pino.Data.ApplicationDbContext db, Guid clubId, TryoutInput input, TryoutEvent? tryout, CancellationToken token)
    {
        if (tryout is null)
        {
            var players = await db.Players.Where(value => value.ClubId == clubId && !value.Archived).OrderBy(value => value.Id).Select(value => value.Id).Take(2001).ToListAsync(token);
            if (players.Count > 2000) { return new SportOutcome.Invalid("This club has more than 2,000 active players. Review the player list before creating a tryout. No players have been left out."); }
            var existingPlacements = (await db.SeasonPlacements.Where(value => value.ClubId == clubId && value.SeasonId == input.SeasonId).Select(value => value.PlayerId).ToListAsync(token)).ToHashSet();
            tryout = new() { Id = input.Id, ClubId = clubId, SeasonId = input.SeasonId };
            db.TryoutEvents.Add(tryout);
            db.Participations.AddRange(players.Select(playerId => new Participation { ClubId = clubId, TryoutId = input.Id, PlayerId = playerId, Revision = 1 }));
            db.SeasonPlacements.AddRange(players.Where(playerId => !existingPlacements.Contains(playerId)).Select(playerId => new SeasonPlacement { ClubId = clubId, SeasonId = input.SeasonId, PlayerId = playerId }));
        }
        tryout.Name = input.Name.Trim();
        tryout.Date = input.Date;
        tryout.Location = input.Location?.Trim() ?? "";
        tryout.Revision++;
        return new SportOutcome.Saved("Tryout saved.", tryout.Id);
    }
}
