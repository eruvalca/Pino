using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Features.Clubs.Models;
using Pino.SharedKernel.Clubs;

namespace Pino.Features.Clubs.Services;

internal sealed partial class ClubService
{
    internal Task<ClubOperationOutcome> SaveDetailsAsync(ClaimsPrincipal actor, Guid clubId, ClubDetailsInput input, CancellationToken cancellationToken) =>
        WriteAsync(actor, async (db, id, ct) =>
        {
            await RequireAdministratorAsync(db, id, clubId, ct);
            if (!ClubRules.ValidDetails(input)) { return new ClubOperationOutcome.Invalid("Enter a club name, sport, city, and valid US state within the displayed limits."); }
            var club = await db.Clubs.SingleAsync(value => value.Id == clubId, ct);
            if (club.Revision != input.Revision) { return new ClubOperationOutcome.Conflict("Club details changed since you opened them. Reload the saved details before trying again."); }
            club.Name = input.Name.Trim();
            club.Sport = input.Sport.Trim();
            club.City = input.City.Trim();
            club.State = input.State;
            club.Revision++;
            return new ClubOperationOutcome.Saved("Club details saved.");
        }, cancellationToken);
}
