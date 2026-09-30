using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Features.Sporting.Data;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal async Task<ImportReport> ImportAsync(ClaimsPrincipal actor, Guid clubId, ImportInput input, CancellationToken ct)
    {
        // Authenticate before parsing potentially large input, then recheck under the write lock.
        await using (var check = await factory.CreateDbContextAsync(ct))
        {
            await RequireMemberAsync(check, actor, clubId, ct);
        }
        var parsed = await PlayerCsv.ParseAsync(input.Csv, ct);
        return await WriteAsync(actor, clubId, async (db, _, token) =>
        {
            var references = parsed.Select(value => value.Player.PlayerReference).ToArray();
            var records = await db.Players.Where(value => value.ClubId == clubId && references.Contains(value.PlayerReference))
                .Select(value => new { value.Id, value.PlayerReference }).ToListAsync(token);
            // Parsing assigns stable IDs outside the execution strategy. Only this batch can
            // have committed these IDs if the connection failed while acknowledging COMMIT.
            if (input.Commit && parsed.Count > 0 && parsed.TrueForAll(value => value.Row.Error is null) &&
                records.Count == parsed.Count && parsed.TrueForAll(value => records.Exists(record => record.Id == value.Player.Id && string.Equals(record.PlayerReference, value.Player.PlayerReference, StringComparison.Ordinal))))
            {
                return new ImportReport(parsed.Select(value => value.Row).ToArray(), Saved: true, $"Imported {parsed.Count} players.");
            }
            var existing = records.Select(value => value.PlayerReference).ToHashSet(StringComparer.Ordinal);
            var rows = parsed.Select(value => existing.Contains(value.Player.PlayerReference)
                ? value.Row with { Error = "This reference already belongs to an active or archived player. No existing records are overwritten." } : value.Row).ToArray();
            if (rows.Length == 0 || rows.Any(row => row.Error is not null)) { return new ImportReport(rows, Saved: false, "Nothing imported. Correct the listed rows and preview the file again."); }
            if (!input.Commit) { return new ImportReport(rows, Saved: false, $"{rows.Length} players are ready. Review them before importing."); }
            foreach (var (player, _) in parsed)
            {
                db.Players.Add(new()
                {
                    Id = player.Id,
                    ClubId = clubId,
                    PlayerReference = player.PlayerReference,
                    FirstName = player.FirstName.Trim(),
                    LastName = player.LastName.Trim(),
                    GraduationYear = player.GraduationYear,
                    Position = player.Position.Trim(),
                    ContactEmail = player.ContactEmail?.Trim() ?? "",
                    Revision = 1,
                });
            }
            return new ImportReport(rows, Saved: true, $"Imported {rows.Length} players.");
        }, ct);
    }
}
