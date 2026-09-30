using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal async Task<ImportReport> ImportAsync(ClaimsPrincipal actor, Guid clubId, ImportInput input, CancellationToken ct)
    {
        await using (var check = await factory.CreateDbContextAsync(ct))
        {
            await RequireMemberAsync(check, actor, clubId, ct);
        }
        var columns = await PlayerCsv.ReadHeadersAsync(input.Csv);
        var mapping = input.Mapping ?? PlayerCsv.SuggestMapping(columns);
        var parsed = await PlayerCsv.ParseAsync(input.Csv, ct, mapping);
        if (input.OperationId == Guid.Empty || input.Resolutions?.GroupBy(value => value.Row).Any(group => group.Count() > 1) == true)
        {
            return new([], Saved: false, "Preview the file again before importing.", columns, mapping);
        }
        ImportReport? attempted = null;
        var report = input.Commit
            ? await WriteAsync(actor, clubId, async (db, actorId, token) =>
            {
                var previous = await db.PlayerImportReceipts.SingleOrDefaultAsync(value => value.Id == input.OperationId, token);
                if (previous is not null)
                {
                    if (previous.ClubId != clubId || !string.Equals(previous.ActorId, actorId, StringComparison.Ordinal))
                    {
                        return new ImportReport([], Saved: false, "This import request is unavailable. Preview the file again.");
                    }
                    return attempted ?? new ImportReport([], Saved: true, "This import request was already processed. The counts below describe that earlier import; open the catalog for current records.",
                        Counts: new(previous.Created, previous.Skipped, previous.Reactivated, 0, 0));
                }
                var preview = await ReviewImportAsync(db, clubId, parsed, input.Resolutions, token);
                if (!preview.CanImport) { return preview; }
                if (preview.Rows.Any(value => value.Disposition is ImportDisposition.Reactivate or ImportDisposition.Replace))
                {
                    await RequireAdministratorAsync(db, actorId, clubId, token);
                }
                var erasures = preview.Rows.Where(value => value.Disposition == ImportDisposition.Replace).Select(value => value.ErasureOperationId!.Value).ToArray();
                if (erasures.Distinct().Count() != erasures.Length || await db.PlayerErasures.AnyAsync(value => erasures.Contains(value.Id), token))
                {
                    return new ImportReport([], Saved: false, "An erasure confirmation was already used. Review this import again.");
                }
                attempted = await ApplyImportAsync(db, actorId, clubId, input.OperationId, parsed, preview.Rows, token);
                return attempted;
            }, ct)
            : await ReadSnapshotAsync(actor, clubId, (db, token) => ReviewImportAsync(db, clubId, parsed, input.Resolutions, token), ct);
        return report with { Columns = columns, Mapping = mapping };
    }
}
