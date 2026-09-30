using System.Diagnostics.CodeAnalysis;
using Pino.Features.Sporting.Services;
using Shouldly;
using Xunit;

namespace Pino.UnitTests.Features.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class PlayerCsvTests
{
    private const string BasicTemplate = "PlayerReference,FirstName,LastName,GraduationYear,Position,ContactEmail\n";
    [Fact]
    public async Task ValidQuotedCsvPreservesNamesAndOptionalFieldsAsync()
    {
        var rows = await PlayerCsv.ParseAsync(BasicTemplate + " p-1 ,\"Avery, Jo\",Morgan,2030,Goalkeeper,parent@example.test\nP-2,Riley,Chen,2031,,\n", TestContext.Current.CancellationToken);
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
        var rows = await PlayerCsv.ParseAsync(BasicTemplate + "p-1,Avery,Morgan,2030,,\nP-1,Riley,Chen,2031,,\n", TestContext.Current.CancellationToken);
        rows[0].Row.Error.ShouldBeNull();
        rows[1].Row.Error.ShouldNotBeNull().ShouldContain("more than once");
        rows[1].Row.Row.ShouldBe(3);
    }

    [Theory]
    [InlineData("Wrong,Header\nA,B\n", "headers")]
    [InlineData("", "nonempty")]
    [InlineData("PlayerReference,FirstName,LastName,GraduationYear,Position,ContactEmail\nP1,Avery,Morgan,nope,,\n", "graduation")]
    [InlineData("PlayerReference,FirstName,LastName,GraduationYear,Position,ContactEmail\nP1,,Morgan,2030,,\n", "names")]
    [InlineData("PlayerReference,FirstName,LastName,GraduationYear,Position,ContactEmail\n,Avery,Morgan,2030,,\n", "reference")]
    [InlineData("PlayerReference,FirstName,LastName,GraduationYear,Position,ContactEmail\nP1,Avery,Morgan,2030,,bad-email\n", "email")]
    public async Task InvalidFileExplainsRowErrorAsync(string csv, string message)
    {
        var rows = await PlayerCsv.ParseAsync(csv, TestContext.Current.CancellationToken);
        rows.ShouldNotBeEmpty();
        rows.ShouldContain(value => value.Row.Error != null && value.Row.Error.Contains(message, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(999, false)]
    [InlineData(1000, false)]
    [InlineData(1001, true)]
    public async Task ImportLimitAcceptsBoundaryAndRejectsNextRowAsync(int count, bool rejected)
    {
        var csv = BasicTemplate + string.Concat(Enumerable.Range(1, count).Select(index => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"P{index},Avery,Morgan,2030,,\n")));
        var rows = await PlayerCsv.ParseAsync(csv, TestContext.Current.CancellationToken);
        rows.Exists(value => value.Row.Error is not null).ShouldBe(rejected);
        rows.Count.ShouldBe(count);
    }

    [Fact]
    public async Task OversizedAndMalformedInputDoesNotBecomePlayersAsync()
    {
        var large = await PlayerCsv.ParseAsync(new('x', 2000001), TestContext.Current.CancellationToken);
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
        var rows = await PlayerCsv.ParseAsync(BasicTemplate + "P1,Avery,Morgan,2030,,\n" + malformed + "\n", TestContext.Current.CancellationToken);
        rows.Count.ShouldBe(2);
        rows[0].Row.Error.ShouldBeNull();
        rows[1].Row.Row.ShouldBe(3);
        rows[1].Row.Error.ShouldNotBeNull().ShouldContain("number of columns");
        new SharedKernel.Sporting.ImportReport(rows.Select(value => value.Row).ToArray(), Saved: false, "").CanImport.ShouldBeFalse();
    }

    [Fact]
    public async Task WideRegistrationExportMapsOnlySelectedFieldsAndIgnoresBlankRowsAsync()
    {
        const string Csv = "Ignore,player_id,player_first_name,player_last_name,Graduation Year,Primary Position,Secondary Position,Middle Name,,\nprivate,opaque-007,Avery,Morgan,2030,Goalkeeper,Midfield,Jo,,\n,,,,,,,,,\n";
        var headers = await PlayerCsv.ReadHeadersAsync(Csv);
        headers.Length.ShouldBe(10);
        var rows = await PlayerCsv.ParseAsync(Csv, TestContext.Current.CancellationToken);
        var row = rows.ShouldHaveSingleItem();
        row.Row.Error.ShouldBeNull();
        row.Row.Name.ShouldBe("Avery Jo Morgan");
        row.Player.PlayerReference.ShouldBe("OPAQUE-007");
        row.Player.MiddleName.ShouldBe("Jo");
        row.Player.Position.ShouldBe("Goalkeeper");
        row.Player.SecondaryPosition.ShouldBe("Midfield");
        row.Player.ContactEmail.ShouldBeNull();
    }

    [Fact]
    public async Task ExplicitMappingCanReorderCustomColumnsAndGenerateAnUnmappedReferenceAsync()
    {
        const string Csv = "Family,Given,Class,Private\nMorgan,Avery,2031,ignore\n";
        var rows = await PlayerCsv.ParseAsync(Csv, TestContext.Current.CancellationToken,
            new(FirstName: 1, LastName: 0, GraduationYear: 2));
        var row = rows.ShouldHaveSingleItem();
        row.Row.Error.ShouldBeNull();
        row.Player.FirstName.ShouldBe("Avery");
        row.Player.LastName.ShouldBe("Morgan");
        row.Player.GraduationYear.ShouldBe(2031);
        SportRules.ValidReference(row.Player.PlayerReference).ShouldBeTrue();
    }

    [Theory]
    [InlineData("Birth Year", "2012")]
    [InlineData("birth_date", "2012-01-01")]
    [InlineData("School Grade (Fall '25)", "7th")]
    public async Task LegacyAgeAndGradeNeverBecomeGraduationYearsAutomaticallyAsync(string column, string value)
    {
        var rows = await PlayerCsv.ParseAsync($"FirstName,LastName,{column}\nAvery,Morgan,{value}\n", TestContext.Current.CancellationToken);
        rows.ShouldHaveSingleItem().Row.Error.ShouldNotBeNull().ShouldContain("graduation year");
    }

    [Theory]
    [InlineData(0, 0, 2)]
    [InlineData(-2, 1, 2)]
    [InlineData(0, 1, 3)]
    [InlineData(0, 1, -1)]
    public async Task DuplicateMissingAndOutOfRangeMappingsAreRejectedAsync(int first, int last, int year)
    {
        var rows = await PlayerCsv.ParseAsync("First,Last,Year\nAvery,Morgan,2030\n", TestContext.Current.CancellationToken,
            new(FirstName: first, LastName: last, GraduationYear: year));
        rows.ShouldHaveSingleItem().Row.Error.ShouldNotBeNull().ShouldContain("Map distinct headers");
    }

    [Fact]
    public async Task SourceIdentifiersThatLookLikeDatesRequireReviewWithoutBeingConvertedAsync()
    {
        var rows = await PlayerCsv.ParseAsync(BasicTemplate + "4567-01-02,Avery,Morgan,2030,,\n", TestContext.Current.CancellationToken);
        var row = rows.ShouldHaveSingleItem();
        row.Player.PlayerReference.ShouldBe("4567-01-02");
        row.Row.Error.ShouldBeNull();
        row.Row.Warning.ShouldNotBeNull().ShouldContain("looks like a date");
    }

    [Fact]
    public async Task DownloadedTemplateIncludesMiddleNameAndSecondaryPositionAsync()
    {
        var rows = await PlayerCsv.ParseAsync(PlayerCsv.Template + "P1,Avery,Morgan,2030,Midfield,,Jo,Forward\n", TestContext.Current.CancellationToken);
        var row = rows.ShouldHaveSingleItem();
        row.Row.Error.ShouldBeNull();
        row.Player.MiddleName.ShouldBe("Jo");
        row.Player.SecondaryPosition.ShouldBe("Forward");
    }

    [Fact]
    public async Task BlankRowsOfAnyWidthAndMissingEmptyTrailingColumnsAreIgnoredAsync()
    {
        const string Csv = "FirstName,LastName,GraduationYear,,\n,\nAvery,Morgan,2030\n,,,,,,,,,\n";
        var rows = await PlayerCsv.ParseAsync(Csv, TestContext.Current.CancellationToken);
        var row = rows.ShouldHaveSingleItem();
        row.Row.Row.ShouldBe(3);
        row.Row.Error.ShouldBeNull();
        row.Player.GraduationYear.ShouldBe(2030);
    }
}
