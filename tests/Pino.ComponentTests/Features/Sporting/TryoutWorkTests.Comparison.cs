using Bunit;
using NSubstitute;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Components;
using Pino.UI.Features.Sporting.Pages;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Sporting;

public sealed partial class TryoutWorkTests
{
    [Fact]
    public async Task ComparisonLoadsSelectedHistoryAndClearsItOnFailureWithoutSavingPreviewAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        var first = data.Roster[0].Player;
        var second = data.Roster[1].Player;
        gateway.GetSeasonReviewAsync(_clubId, data.Season.Id, Arg.Any<CancellationToken>()).Returns(new SeasonReview(data.Season,
            [new(data.Teams[0] with { RosterTarget = 1 }, [first]), new(data.Teams[1], [])], [data.Tryout], [second], 2));
        gateway.GetPlayerAsync(_clubId, first.Id, Arg.Any<CancellationToken>()).Returns(new PlayerDetail(first, [], [],
            [new(_tryoutId, "Current tryout", "Spring", data.Tryout.Date, "17", false, false, DecisionKind.Awaiting, null)]));
        gateway.GetNotebookAsync(_clubId, _tryoutId, first.Id, Arg.Any<CancellationToken>()).Returns(new PlayerNotebook(first.Id, false,
            [new(Guid.NewGuid(), first.Id, "First player's private observation", "Coach", DateTimeOffset.UtcNow, null, false)], [], []));
        gateway.GetPlayerAsync(_clubId, second.Id, Arg.Any<CancellationToken>()).Returns(Task.FromException<PlayerDetail>(new HttpRequestException("Unavailable")));
        var page = context.Render<TeamComparison>(parameters => parameters.Add(value => value.ClubId, _clubId).Add(value => value.SeasonId, data.Season.Id));
        await gateway.DidNotReceive().GetNotebookAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await page.FindAll(".comparison-player")[0].ClickAsync();
        page.Markup.ShouldContain("First player's private observation");
        var previews = page.FindAll(".comparison-notebook fieldset button");
        previews[1].HasAttribute("disabled").ShouldBeTrue();
        await previews[0].ClickAsync();
        page.Markup.ShouldContain("Preview only. No placement has been saved.");
        await page.Find(".comparison-preview-coverage").ClickAsync();
        page.Find(".comparison-surface").GetAttribute("data-view").ShouldBe("rosters");
        page.Find(".comparison-player[aria-pressed='true']").TextContent.ShouldContain(first.FullName);
        await page.Find(".comparison-return").ClickAsync();
        page.Find(".comparison-surface").GetAttribute("data-view").ShouldBe("notebook");
        page.Markup.ShouldContain("Preview only. No placement has been saved.");
        await gateway.DidNotReceive().DecideAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DecisionInput>(), Arg.Any<CancellationToken>());
        await page.FindAll(".comparison-player")[1].ClickAsync();
        page.Markup.ShouldNotContain("First player's private observation");
        page.Markup.ShouldNotContain("Preview only. No placement has been saved.");
        page.Markup.ShouldContain("Player history is unavailable.");
    }

    [Fact]
    public async Task ComparisonRosterPagesLargeListsAndRetainsSelectionAsync()
    {
        await using var context = new BunitContext();
        var template = Data().Roster[0].Player;
        var players = Enumerable.Range(0, 1000).Select(value => template with { Id = Guid.NewGuid(), FirstName = $"Player {value:D4}" }).ToArray();
        Guid selected = Guid.Empty;
        var page = context.Render<ComparisonRoster>(parameters => parameters.Add(value => value.Players, players).Add(value => value.Label, "Blue")
            .Add(value => value.Selected, id => selected = id));
        page.FindAll(".comparison-player").Count.ShouldBe(50);
        await page.FindAll(".pagination button")[1].ClickAsync();
        page.Find(".comparison-player").TextContent.ShouldContain("Player 0050");
        await page.Find(".comparison-player").ClickAsync();
        selected.ShouldBe(players[50].Id);
        page.Find(".pagination").TextContent.ShouldContain("Page 2 of 20");
    }
}
