using System.Diagnostics.CodeAnalysis;
using Bunit;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Components;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class ImportColumnMapperTests
{
    [Fact]
    public async Task MappingChangesNotifyOwnerWithoutMutatingSuppliedMappingAsync()
    {
        await using var context = new BunitContext();
        var supplied = new CsvColumnMapping(FirstName: 0, LastName: 1);
        CsvColumnMapping? changed = null;
        var component = context.Render<ImportColumnMapper>(parameters => parameters
            .Add(value => value.Columns, ["Given", "Family", "Unrelated", "Graduation"])
            .Add(value => value.Mapping, supplied)
            .Add(value => value.MappingChanged, value => { changed = value; }));

        await component.Find("#map-year").ChangeAsync("3");

        changed.ShouldNotBeNull().GraduationYear.ShouldBe(3);
        changed.FirstName.ShouldBe(0);
        changed.LastName.ShouldBe(1);
        supplied.GraduationYear.ShouldBe(-1);
        component.Find("label[for='map-year']").TextContent.ShouldBe("Graduation year");
    }

    [Fact]
    public async Task EmptyExportColumnsCannotBeSelectedAndBusyMappingIsDisabledAsync()
    {
        await using var context = new BunitContext();
        var component = context.Render<ImportColumnMapper>(parameters => parameters
            .Add(value => value.Columns, ["First", "", "Last", " ", "Year"])
            .Add(value => value.Mapping, new CsvColumnMapping())
            .Add(value => value.Disabled, true));

        component.Find("fieldset").HasAttribute("disabled").ShouldBeTrue();
        component.FindAll("#map-year option").Select(value => value.GetAttribute("value")).ShouldBe(["-1", "0", "2", "4"]);
        component.Markup.ShouldContain("birth year and school grade cannot replace it");
    }
}
