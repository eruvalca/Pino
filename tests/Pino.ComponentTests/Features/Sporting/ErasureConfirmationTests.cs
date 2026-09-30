using System.Diagnostics.CodeAnalysis;
using Bunit;
using Pino.UI.Features.Sporting.Components;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class ErasureConfirmationTests
{
    [Fact]
    public async Task TypingAndAcknowledgementAreSeparateExplicitActionsAsync()
    {
        await using var context = new BunitContext();
        string? name = null;
        var acknowledged = false;
        var component = context.Render<ErasureConfirmation>(parameters => parameters
            .Add(value => value.FullName, "Avery Jo Reed")
            .Add(value => value.InputId, "erase")
            .Add(value => value.ConfirmationChanged, value => { name = value; })
            .Add(value => value.AcknowledgeHistoryChanged, value => { acknowledged = value; }));
        component.Find("input[type=checkbox]").HasAttribute("checked").ShouldBeFalse();
        component.Find("label[for=erase-name]").TextContent.ShouldBe("Type Avery Jo Reed to confirm");
        component.Markup.ShouldContain("copies in closed results");
        await component.Find("#erase-name").InputAsync("Avery Jo Reed");
        name.ShouldBe("Avery Jo Reed");
        acknowledged.ShouldBeFalse();
        await component.Find("input[type=checkbox]").ChangeAsync(true);
        acknowledged.ShouldBeTrue();
    }
}
