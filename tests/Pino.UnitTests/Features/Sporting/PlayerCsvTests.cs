using System.Diagnostics.CodeAnalysis;
using Pino.Features.Sporting.Services;
using Shouldly;
using Xunit;

namespace Pino.UnitTests.Features.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class PlayerCsvTests
{
    [Fact]
    public async Task ValidQuotedCsvPreservesNamesAndOptionalFieldsAsync()
    {
        var rows = await PlayerCsv.ParseAsync(PlayerCsv.Template + " p-1 ,\"Avery, Jo\",Morgan,2030,Goalkeeper,parent@example.test\nP-2,Riley,Chen,2031,,\n", TestContext.Current.CancellationToken);
        rows.Count.ShouldBe(2);
        rows[0].Player.FirstName.ShouldBe("Avery, Jo");
        rows[0].Player.PlayerReference.ShouldBe("P-1");
        rows[0].Player.GraduationYear.ShouldBe(2030);
        rows[0].Player.ContactEmail.ShouldBe("parent@example.test");
        rows[0].Row.Row.ShouldBe(2);
        rows[1].Player.ContactEmail.ShouldBeNull();
        rows.ShouldAllBe(value => value.Row.Error == null);
    }

    [Fact]
    public async Task DuplicateReferencesAreReportedCaseInsensitivelyAsync()
    {
        var rows = await PlayerCsv.ParseAsync(PlayerCsv.Template + "p-1,Avery,Morgan,2030,,\nP-1,Riley,Chen,2031,,\n", TestContext.Current.CancellationToken);
        rows[0].Row.Error.ShouldBeNull();
        rows[1].Row.Error.ShouldNotBeNull().ShouldContain("more than once");
        rows[1].Row.Row.ShouldBe(3);
    }

    [Theory]
    [InlineData("Wrong,Header\nA,B\n", "headers")]
    [InlineData("", "nonempty")]
    [InlineData("PlayerReference,FirstName,LastName,GraduationYear,Position,ContactEmail\nP1,Avery,Morgan,nope,,\n", "graduation")]
    [InlineData("PlayerReference,FirstName,LastName,GraduationYear,Position,ContactEmail\nP1,,Morgan,2030,,\n", "names")]
    [InlineData("PlayerReference,FirstName,LastName,GraduationYear,Position,ContactEmail\nP1,Avery,Morgan,2030,,bad-email\n", "email")]
    public async Task InvalidFileExplainsRowErrorAsync(string csv, string message)
    {
        var rows = await PlayerCsv.ParseAsync(csv, TestContext.Current.CancellationToken);
        rows.ShouldNotBeEmpty();
        rows.ShouldContain(value => value.Row.Error != null && value.Row.Error.Contains(message, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(500, false)]
    [InlineData(501, true)]
    public async Task ImportLimitAcceptsBoundaryAndRejectsNextRowAsync(int count, bool rejected)
    {
        var csv = PlayerCsv.Template + string.Concat(Enumerable.Range(1, count).Select(index => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"P{index},Avery,Morgan,2030,,\n")));
        var rows = await PlayerCsv.ParseAsync(csv, TestContext.Current.CancellationToken);
        rows.Exists(value => value.Row.Error is not null).ShouldBe(rejected);
        rows.Count.ShouldBe(count);
    }

    [Fact]
    public async Task OversizedAndMalformedInputDoesNotBecomePlayersAsync()
    {
        var large = await PlayerCsv.ParseAsync(new('x', 500001), TestContext.Current.CancellationToken);
        large.ShouldHaveSingleItem().Row.Error.ShouldNotBeNull();
        var malformed = await PlayerCsv.ParseAsync(PlayerCsv.Template + "P1,Avery\n", TestContext.Current.CancellationToken);
        malformed.ShouldContain(value => value.Row.Error != null);
        var headerOnly = await PlayerCsv.ParseAsync(PlayerCsv.Template, TestContext.Current.CancellationToken);
        headerOnly.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("P2,Ada,Lee,2030,Midfield,,ada@example.test")]
    [InlineData("P2,Ada,Lee,2030,Midfield")]
    public async Task InconsistentRowWidthRejectsTheBatchAsync(string malformed)
    {
        var rows = await PlayerCsv.ParseAsync(PlayerCsv.Template + "P1,Avery,Morgan,2030,,\n" + malformed + "\n", TestContext.Current.CancellationToken);
        rows.Count.ShouldBe(2);
        rows[0].Row.Error.ShouldBeNull();
        rows[1].Row.Row.ShouldBe(3);
        rows[1].Row.Error.ShouldNotBeNull().ShouldContain("number of columns");
        new SharedKernel.Sporting.ImportReport(rows.Select(value => value.Row).ToArray(), Saved: false, "").CanImport.ShouldBeFalse();
    }
}
