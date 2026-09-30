using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Pino.SharedKernel.Sporting;

namespace Pino.Features.Sporting.Services;

internal static class PlayerCsv
{
    internal const string Template = "PlayerReference,FirstName,LastName,GraduationYear,Position,ContactEmail\n";
    private static readonly string[] _headers = ["PlayerReference", "FirstName", "LastName", "GraduationYear", "Position", "ContactEmail"];

    internal static async Task<List<(PlayerInput Player, ImportRow Row)>> ParseAsync(string text, CancellationToken ct)
    {
        var result = new List<(PlayerInput Player, ImportRow Row)>();
        if (string.IsNullOrWhiteSpace(text) || text.Length > 500000)
        {
            return [(new(), new(1, "", "", 0, "Choose a nonempty CSV of no more than 500,000 characters."))];
        }
        using var reader = new StringReader(text);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => args.Header.Trim().ToUpperInvariant(),
            ExceptionMessagesContainRawData = false,
            DetectColumnCountChanges = true,
        });
        try
        {
            if (!await csv.ReadAsync()) { return result; }
            csv.ReadHeader();
            if (csv.HeaderRecord is not { Length: 6 } header || !_headers.All(name => header.Contains(name, StringComparer.OrdinalIgnoreCase)))
            {
                return [(new(), new(1, "", "", 0, "Use these six headers: " + string.Join(", ", _headers)))];
            }
            var references = new HashSet<string>(StringComparer.Ordinal);
            while (await csv.ReadAsync())
            {
                ct.ThrowIfCancellationRequested();
                if (result.Count >= 500)
                {
                    result.Add((new(), new(csv.Parser.Row, "", "", 0, "Import at most 500 players at a time.")));
                    break;
                }
                var yearText = csv.GetField("GraduationYear");
                var year = int.TryParse(yearText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedYear) ? parsedYear : 0;
                var player = new PlayerInput
                {
                    PlayerReference = SportRules.Reference(csv.GetField("PlayerReference")),
                    FirstName = csv.GetField("FirstName") ?? "",
                    LastName = csv.GetField("LastName") ?? "",
                    GraduationYear = year,
                    Position = csv.GetField("Position") ?? "",
                    ContactEmail = csv.GetField("ContactEmail"),
                };
                if (string.IsNullOrEmpty(player.ContactEmail)) { player.ContactEmail = null; }
                string? error = null;
                if (!SportRules.ValidPlayer(player)) { error = "Check names, reference, graduation year (2000–2100), position length and email format."; }
                else if (!references.Add(player.PlayerReference)) { error = "This reference appears more than once in the file."; }
                result.Add((player, new(csv.Parser.Row, player.PlayerReference, $"{player.FirstName} {player.LastName}", year, error)));
            }
        }
        catch (CsvHelperException)
        {
            result.Add((new(), new(csv.Parser.Row, "", "", 0, "Malformed CSV. Check quoted fields and the number of columns.")));
        }
        return result;
    }
}
