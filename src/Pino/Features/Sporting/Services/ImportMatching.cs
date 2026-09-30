using Pino.Features.Sporting.Data;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal static class ImportMatching
{
    internal static bool SameNames(PlayerInput incoming, Player existing) =>
        incoming.GraduationYear == existing.GraduationYear && EqualName(incoming.FirstName, existing.FirstName) &&
        EqualName(incoming.LastName, existing.LastName) &&
        (string.IsNullOrWhiteSpace(incoming.MiddleName) || string.IsNullOrWhiteSpace(existing.MiddleName) || EqualName(incoming.MiddleName, existing.MiddleName));

    private static bool EqualName(string left, string right) =>
        string.Equals(string.Join(' ', left.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)),
            string.Join(' ', right.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)), StringComparison.OrdinalIgnoreCase);

    internal static ImportRow Review(PlayerInput incoming, ImportRow row, IReadOnlyList<Player> records, ImportResolution? resolution)
    {
        if (row.Error is not null) { return row with { Disposition = ImportDisposition.Error }; }
        var candidates = records.Where(value => string.Equals(value.PlayerReference, incoming.PlayerReference, StringComparison.Ordinal) || SameNames(incoming, value)).ToArray();
        row = row with
        {
            Candidates = candidates.Select(value => new ImportCandidate(value.Id,
            string.Join(' ', new[] { value.FirstName, value.MiddleName, value.LastName }.Where(part => !string.IsNullOrWhiteSpace(part))), value.GraduationYear, value.Archived, value.Revision)).ToArray()
        };
        if (resolution?.Action == ImportDisposition.Skip) { return row with { Disposition = ImportDisposition.Skip }; }
        if (candidates.Length == 0)
        {
            return resolution is null ? row : row with { Disposition = ImportDisposition.Review, Error = "The candidate changed. Preview and review this row again." };
        }
        if (candidates.Any(value => !value.Archived))
        {
            return row with
            {
                Disposition = ImportDisposition.Skip,
                PlayerId = candidates.First(value => !value.Archived).Id,
                Warning = "An active player has this reference or matching names and graduation year. This row will be skipped. Update that record in Pino; if they are different people, add the new player manually after reviewing the match."
            };
        }
        var selected = candidates.SingleOrDefault(value => value.Id == resolution?.CandidateId);
        if (selected is null || selected.Revision != resolution?.Revision)
        {
            return row with { Disposition = ImportDisposition.Review, Warning = "Review the archived candidates. Confirm the same person to restore their existing record, or skip this row." };
        }
        if (resolution?.Action == ImportDisposition.Reactivate)
        {
            return row with { Disposition = ImportDisposition.Reactivate, PlayerId = selected.Id, Warning = "The existing record will be restored with all its history. CSV fields will not update it." };
        }
        if (resolution?.Action == ImportDisposition.Replace && candidates.Length == 1 && SameNames(incoming, selected) &&
            resolution.ErasureOperationId != Guid.Empty && resolution.AcknowledgeHistory &&
            string.Equals(resolution.Confirmation?.Trim(), row.Candidates[0].Name, StringComparison.Ordinal))
        {
            return row with
            {
                Disposition = ImportDisposition.Replace,
                PlayerId = selected.Id,
                ErasureOperationId = resolution.ErasureOperationId,
                Warning = "A new player will be created and this archived candidate's records, photos and linked history permanently erased."
            };
        }
        return row with { Disposition = ImportDisposition.Review, Warning = "Choose one matching archived player. Confirm they are different people, type the archived player's full name and confirm permanent deletion. Otherwise skip this row." };
    }
}
