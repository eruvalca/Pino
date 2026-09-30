using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Pino.Features.Sporting.Models;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal sealed partial class SportService
{
    internal Task<SportOutcome> SaveBibNumberAsync(ClaimsPrincipal actor, Guid clubId, Guid tryoutId, BibNumberInput input, CancellationToken ct) =>
        WriteAsync<SportOutcome>(actor, clubId, async (db, _, token) =>
        {
            var tryout = await TryoutAsync(db, clubId, tryoutId, token);
            if (tryout is null || await SeasonAsync(db, clubId, tryout.SeasonId, token) is not { Archived: false })
            {
                return new SportOutcome.Invalid("Reopen the season before changing bib numbers.");
            }
            var entry = await db.Participations.SingleOrDefaultAsync(value => value.ClubId == clubId && value.TryoutId == tryoutId && value.PlayerId == input.PlayerId, token);
            if (tryout.Closed) { return new SportOutcome.Invalid("Reopen the tryout before changing bib numbers."); }
            if (entry is null || entry.Removed) { return new SportOutcome.Invalid("Choose a player actively enrolled in this tryout."); }
            var bib = (input.BibNumber ?? "").Trim();
            if (bib.Length > 20) { return new SportOutcome.Invalid("Use a bib number or label of up to 20 characters."); }
            // Comparing the saved bib under the shared write lock protects bib edits without
            // invalidating independent decision drafts. An unchanged retry acknowledges a saved edit.
            if (string.Equals(entry.Bib, bib, StringComparison.Ordinal)) { return new SportOutcome.Saved("Bib number saved.", input.PlayerId); }
            if (!string.Equals(entry.Bib, input.ExpectedBibNumber, StringComparison.Ordinal))
            {
                return new SportOutcome.Conflict("This bib number changed. Reload the saved bib number before editing again.");
            }
            if (bib.Length > 0 && await db.Participations.AnyAsync(value => value.ClubId == clubId && value.TryoutId == tryoutId && !value.Removed && value.Bib == bib, token))
            {
                return new SportOutcome.Invalid("That bib number is already assigned in this tryout. Choose another or leave it empty.");
            }
            entry.Bib = bib;
            tryout.Revision++;
            return new SportOutcome.Saved(bib.Length == 0 ? "Bib number cleared." : "Bib number saved.", input.PlayerId);
        }, ct);
}
