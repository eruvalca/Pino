using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal static class PlayerCsv
{
    internal const int MaxPlayers = 1000;
    internal const int MaxCharacters = 2000000;
    internal const string Template = "PlayerReference,FirstName,LastName,GraduationYear,Position,ContactEmail,MiddleName,SecondaryPosition\n";

    private static CsvConfiguration Configuration() => new(CultureInfo.InvariantCulture)
    {
        TrimOptions = TrimOptions.Trim,
        ExceptionMessagesContainRawData = false,
    };

    internal static async Task<string[]> ReadHeadersAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > MaxCharacters) { return []; }
        using var reader = new StringReader(text);
        using var csv = new CsvReader(reader, Configuration());
        try
        {
            if (!await csv.ReadAsync()) { return []; }
            csv.ReadHeader();
            return csv.HeaderRecord ?? [];
        }
        catch (CsvHelperException) { return []; }
    }

    internal static CsvColumnMapping SuggestMapping(IReadOnlyList<string> headers) => new(
        Find(headers, "PlayerReference", "player_id"), Find(headers, "FirstName", "player_first_name"),
        Find(headers, "MiddleName", "player_middle_name"), Find(headers, "LastName", "player_last_name"),
        Find(headers, "GraduationYear", "GradYear", "HighSchoolGraduationYear"),
        Find(headers, "Position", "PrimaryPosition"), Find(headers, "SecondaryPosition"),
        Find(headers, "ContactEmail", "account_email"));

    private static int Find(IReadOnlyList<string> headers, params string[] aliases)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            var normalized = string.Concat(headers[index].Where(char.IsAsciiLetterOrDigit));
            if (aliases.Any(alias => string.Equals(normalized, string.Concat(alias.Where(char.IsAsciiLetterOrDigit)), StringComparison.OrdinalIgnoreCase))) { return index; }
        }
        return -1;
    }

    internal static async Task<List<(PlayerInput Player, ImportRow Row)>> ParseAsync(string text, CancellationToken ct, CsvColumnMapping? mapping = null)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > MaxCharacters)
        {
            return [(new(), new(1, "", "", 0, "Choose a nonempty CSV of no more than 2,000,000 characters."))];
        }
        var result = new List<(PlayerInput Player, ImportRow Row)>();
        using var reader = new StringReader(text);
        using var csv = new CsvReader(reader, Configuration());
        try
        {
            if (!await csv.ReadAsync()) { return result; }
            csv.ReadHeader();
            var headers = csv.HeaderRecord ?? [];
            mapping ??= SuggestMapping(headers);
            if (mapping.FirstName < 0 || mapping.LastName < 0 || mapping.GraduationYear < 0 ||
                mapping.Indices.Any(index => index < -1 || index >= headers.Length) ||
                mapping.Indices.Where(index => index >= 0).Distinct().Count() != mapping.Indices.Count(index => index >= 0))
            {
                return [(new(), new(1, "", "", 0, "Map distinct headers for first name, last name and graduation year. Birth year and school grade are not graduation years."))];
            }
            var references = new HashSet<string>(StringComparer.Ordinal);
            while (await csv.ReadAsync())
            {
                ct.ThrowIfCancellationRequested();
                if (csv.Parser.Record?.All(string.IsNullOrWhiteSpace) == true) { continue; }
                var requiredWidth = Array.FindLastIndex(headers, value => !string.IsNullOrWhiteSpace(value)) + 1;
                if (csv.Parser.Count < requiredWidth || csv.Parser.Count > headers.Length)
                {
                    result.Add((new(), new(csv.Parser.Row, "", "", 0, "Malformed CSV. Check quoted fields and the number of columns.")));
                    break;
                }
                if (result.Count >= MaxPlayers)
                {
                    result.Add((new(), new(csv.Parser.Row, "", "", 0, "Import at most 1,000 players at a time.")));
                    break;
                }
                var player = ReadPlayer(csv, mapping);
                string? error = null;
                if (!SportRules.ValidPlayer(player)) { error = "Check names, reference, graduation year (2000–2100), position length and email format."; }
                else if (!references.Add(player.PlayerReference)) { error = "This reference appears more than once in the file."; }
                var warning = DateOnly.TryParseExact(player.PlayerReference, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                    ? "This source identifier looks like a date. Check that the spreadsheet has not reformatted it before importing." : null;
                var name = string.Join(" ", new[] { player.FirstName, player.MiddleName, player.LastName }.Where(value => value.Length > 0));
                result.Add((player, new(csv.Parser.Row, player.PlayerReference, name, player.GraduationYear, error, warning)));
            }
        }
        catch (CsvHelperException)
        {
            result.Add((new(), new(csv.Parser.Row, "", "", 0, "Malformed CSV. Check quoted fields and the number of columns.")));
        }
        return result;
    }

    private static PlayerInput ReadPlayer(CsvReader csv, CsvColumnMapping mapping)
    {
        string Field(int index) => index < 0 ? "" : csv.GetField(index) ?? "";
        var player = new PlayerInput
        {
            FirstName = Field(mapping.FirstName),
            MiddleName = Field(mapping.MiddleName),
            LastName = Field(mapping.LastName),
            GraduationYear = int.TryParse(Field(mapping.GraduationYear), NumberStyles.None, CultureInfo.InvariantCulture, out var year) ? year : 0,
            Position = Field(mapping.Position),
            SecondaryPosition = Field(mapping.SecondaryPosition),
            ContactEmail = Field(mapping.ContactEmail),
        };
        if (mapping.PlayerReference >= 0) { player.PlayerReference = SportRules.Reference(Field(mapping.PlayerReference)); }
        return player;
    }
}
