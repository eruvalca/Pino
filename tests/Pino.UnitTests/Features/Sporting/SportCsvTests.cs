using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using CsvHelper;
using Pino.Features.Sporting.Services;
using Shouldly;
using Xunit;

namespace Pino.UnitTests.Features.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class SportCsvTests
{
    [Fact]
    public void ExportPreservesUnicodeQuotesNewlinesAndEmptyCells()
    {
        var bytes = SportCsv.Write(["Name", "Position", "Optional"], [["Jo, \"Émile\"", "First line\nSecond line", ""]]);
        bytes.Take(3).ShouldBe(new byte[] { 0xEF, 0xBB, 0xBF });
        using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read().ShouldBeTrue();
        csv.ReadHeader();
        csv.HeaderRecord.ShouldBe(["Name", "Position", "Optional"]);
        csv.Read().ShouldBeTrue();
        csv.GetField("Name").ShouldBe("Jo, \"Émile\"");
        csv.GetField("Position").ShouldBe("First line\nSecond line");
        csv.GetField("Optional").ShouldBeEmpty();
        csv.Read().ShouldBeFalse();
    }

    [Theory]
    [InlineData("=1+1", "'=1+1")]
    [InlineData("  =1+1", "'  =1+1")]
    [InlineData("+1+1", "'+1+1")]
    [InlineData("-1+1", "'-1+1")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    [InlineData("\ttext", "'\ttext")]
    [InlineData("\rtext", "'\rtext")]
    [InlineData("\ntext", "'\ntext")]
    [InlineData("'literal", "'literal")]
    [InlineData("North-side", "North-side")]
    [InlineData("", "")]
    public void ExportTreatsSpreadsheetFormulaTriggersAsLiteralText(string input, string expected)
    {
        using var reader = new StreamReader(new MemoryStream(SportCsv.Write(["Value"], [[input]])), Encoding.UTF8);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Read().ShouldBeTrue();
        csv.ReadHeader();
        csv.Read().ShouldBeTrue();
        csv.GetField("Value").ShouldBe(expected);
    }

    [Fact]
    public void EmptyExportStillNamesEveryColumn()
    {
        Encoding.UTF8.GetString(SportCsv.Write(["FirstName", "GraduationYear"], [])).ShouldBe("\uFEFF\"FirstName\",\"GraduationYear\"\r\n");
    }
}
