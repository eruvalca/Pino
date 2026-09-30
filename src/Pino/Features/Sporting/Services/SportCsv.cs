using System.Globalization;
using System.Text;
using CsvHelper;

namespace Pino.Features.Sporting.Services;

internal static class SportCsv
{
    internal static byte[] Write(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        foreach (var header in headers) { csv.WriteField(header, shouldQuote: true); }
        csv.NextRecord();
        foreach (var row in rows)
        {
            foreach (var cell in row) { csv.WriteField(Literal(cell), shouldQuote: true); }
            csv.NextRecord();
        }
        csv.Flush();
        // A BOM lets common spreadsheet applications recognize UTF-8 names reliably.
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetPreamble().Concat(Encoding.UTF8.GetBytes(writer.ToString())).ToArray();
    }

    private static string Literal(string value)
    {
        var significant = value.AsSpan().TrimStart();
        return (significant.Length > 0 && significant[0] is '=' or '+' or '-' or '@') || (value.Length > 0 && value[0] is '\t' or '\r' or '\n') ? "'" + value : value;
    }
}
