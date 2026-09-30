using Bunit;
using NSubstitute;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Sporting;

public sealed partial class TryoutWorkTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RefreshDismissesEnrollmentWhenTryoutClosesOrSeasonArchivesAsync(bool archived)
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        var candidate = data.Roster[0].Player with { Id = Guid.NewGuid(), FirstName = "Morgan" };
        gateway.GetPlayersAsync(_clubId, "", false, 0, Arg.Any<CancellationToken>()).Returns(new PlayerPage([candidate], 0, HasMore: false));
        var page = Render(context);
        await page.Find("#player-note").InputAsync("Keep this draft through refresh.");
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Add players", StringComparison.Ordinal)).ClickAsync();
        await page.WaitForAssertionAsync(() => page.Find("button[aria-label='Add Morgan Rivera to tryout']").HasAttribute("disabled").ShouldBeFalse());

        gateway.GetTryoutAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>()).Returns(data with
        {
            Tryout = data.Tryout with { Closed = !archived },
            Season = data.Season with { Archived = archived },
        });
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Refresh saved decisions", StringComparison.Ordinal)).ClickAsync();
        await page.WaitForAssertionAsync(() =>
        {
            page.FindAll("#enrollment-heading").ShouldBeEmpty();
            page.FindAll("button[aria-label='Add Morgan Rivera to tryout']").ShouldBeEmpty();
            page.FindAll("button").Single(value => string.Equals(value.TextContent, "Add players", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeTrue();
            page.FindAll(".player-notebook fieldset").ShouldAllBe(fieldset => fieldset.HasAttribute("disabled"));
            page.Find("#player-note").GetAttribute("value").ShouldBe("Keep this draft through refresh.");
        });

        gateway.GetTryoutAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>()).Returns(data);
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Refresh saved decisions", StringComparison.Ordinal)).ClickAsync();
        await page.WaitForAssertionAsync(() =>
        {
            page.FindAll("#enrollment-heading").ShouldBeEmpty();
            page.FindAll("button").Single(value => string.Equals(value.TextContent, "Add players", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeFalse();
        });
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Add players", StringComparison.Ordinal)).ClickAsync();
        await page.WaitForAssertionAsync(() => page.Find("button[aria-label='Add Morgan Rivera to tryout']").HasAttribute("disabled").ShouldBeFalse());
        await gateway.DidNotReceive().EnrollAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<EnrollmentInput>(), Arg.Any<CancellationToken>());
    }
}
