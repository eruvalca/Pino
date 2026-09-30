using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Features.Clubs.Data;
using Pino.Features.Sporting.Data;
using Pino.Features.Sporting.Models;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal async Task<SportOutcome> SavePlayerAsync(ClaimsPrincipal actor, Guid clubId, PlayerInput input, CancellationToken ct)
    {
        if (!SportRules.ValidPlayer(input)) { return new SportOutcome.Invalid("Enter names, a graduation year from 2000 to 2100, and a unique player reference using letters, numbers, dashes or underscores."); }
        string? photoKey = null;
        if (input.Photo is not null && !input.RemovePhoto)
        {
            await using var staging = await factory.CreateDbContextAsync(ct);
            await RequireMemberAsync(staging, actor, clubId, ct);
            byte[] jpeg;
            try { jpeg = PlayerPhoto.Normalize(input.Photo); }
            catch (Exception exception) when (exception is FormatException or InvalidDataException)
            {
                return new SportOutcome.Invalid("Choose a valid JPEG or PNG up to 5 MB and 4096 pixels on each side.");
            }
            photoKey = $"{Guid.NewGuid():N}.jpg";
            staging.PhotoDeletions.Add(new() { PhotoKey = photoKey, NotBefore = time.GetUtcNow().AddHours(1) });
            await staging.SaveChangesAsync(ct);
            await photos.UploadAsync(photoKey, jpeg, ct);
        }
        return await WriteAsync<SportOutcome>(actor, clubId, async (db, _, token) =>
        {
            var player = await db.Players.SingleOrDefaultAsync(value => value.ClubId == clubId && value.Id == input.Id, token);
            if ((player?.Revision ?? 0) != input.Revision) { return new SportOutcome.Conflict(StaleMessage); }
            var reference = SportRules.Reference(input.PlayerReference);
            if (await db.Players.AnyAsync(value => value.ClubId == clubId && value.Id != input.Id && value.PlayerReference == reference, token))
            {
                return new SportOutcome.Invalid("This player reference already exists, including archived players. Open that record or use a different reference.");
            }
            if (await (from placement in db.SeasonPlacements
                       join team in db.SportTeams on placement.TeamId equals team.Id
                       where placement.ClubId == clubId && placement.PlayerId == input.Id && input.GraduationYear < team.GraduationYear
                       select placement.PlayerId).AnyAsync(token))
            {
                return new SportOutcome.Invalid("This graduation year is too early for one of the player's teams. Change that team placement first.");
            }
            if (player is null)
            {
                player = new() { Id = input.Id, ClubId = clubId };
                db.Players.Add(player);
            }
            UpdatePlayerFields(player, input, reference);
            if (photoKey is not null || input.RemovePhoto)
            {
                if (player.PhotoKey is { } old) { db.PhotoDeletions.Add(new() { PhotoKey = old, NotBefore = time.GetUtcNow() }); }
                player.PhotoKey = input.RemovePhoto ? null : photoKey;
                if (photoKey is not null)
                {
                    var cleanup = await db.PhotoDeletions.FindAsync([photoKey], token);
                    if (cleanup is not null) { db.PhotoDeletions.Remove(cleanup); }
                }
            }
            return new SportOutcome.Saved("Player saved.", player.Id);
        }, ct);
    }

    private static void UpdatePlayerFields(Player player, PlayerInput input, string reference)
    {
        player.PlayerReference = reference;
        player.FirstName = input.FirstName.Trim();
        player.MiddleName = input.MiddleName?.Trim() ?? "";
        player.LastName = input.LastName.Trim();
        player.GraduationYear = input.GraduationYear;
        player.Position = input.Position?.Trim() ?? "";
        player.SecondaryPosition = input.SecondaryPosition?.Trim() ?? "";
        player.ContactEmail = input.ContactEmail?.Trim() ?? "";
        player.Archived = input.Archived;
        player.Revision++;
    }
}
