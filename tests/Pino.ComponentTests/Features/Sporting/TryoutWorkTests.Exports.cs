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
    public async Task PlayerHistoryLoadsOnlyChosenTryoutAndClearsPriorDetailsOnFailureAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        var player = data.Roster[0].Player;
        var otherTryout = Guid.NewGuid();
        gateway.GetPlayerAsync(_clubId, player.Id, Arg.Any<CancellationToken>()).Returns(new PlayerDetail(player, [], [],
            [new(_tryoutId, "Original tryout", "Spring", data.Tryout.Date, "17", false, false, DecisionKind.Placed, "Blue"), new(otherTryout, "Removed tryout", "Autumn", data.Tryout.Date, "", true, false, DecisionKind.Awaiting, null)]));
        gateway.GetNotebookAsync(_clubId, _tryoutId, player.Id, Arg.Any<CancellationToken>()).Returns(new PlayerNotebook(player.Id, false,
            [new(Guid.NewGuid(), player.Id, "Original tryout only", "Coach", DateTimeOffset.UtcNow, null, false)], [], []));
        gateway.GetNotebookAsync(_clubId, otherTryout, player.Id, Arg.Any<CancellationToken>()).Returns(Task.FromException<PlayerNotebook>(new HttpRequestException("Unavailable")));
        var page = context.Render<PlayerRecord>(parameters => parameters.Add(value => value.ClubId, _clubId).Add(value => value.PlayerId, player.Id));
        await gateway.DidNotReceive().GetNotebookAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await page.Find("#history-tryout").ChangeAsync(_tryoutId.ToString());
        page.Markup.ShouldContain("Original tryout only");
        await page.Find("#history-tryout").ChangeAsync(otherTryout.ToString());
        page.Markup.ShouldNotContain("Original tryout only");
        page.Markup.ShouldContain("History is unavailable");
        page.Markup.ShouldNotContain("Download player personal data");
        gateway.GetNotebookAsync(_clubId, otherTryout, player.Id, Arg.Any<CancellationToken>()).Returns(new PlayerNotebook(player.Id, true, [], [], [],
            [new(Guid.NewGuid(), true, "Wrong session enrollment", "Coach", DateTimeOffset.UtcNow, "14")]));
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Reload this history", StringComparison.Ordinal)).ClickAsync();
        page.Markup.ShouldContain("Wrong session enrollment");
        page.Markup.ShouldContain("Removed from active roster");
    }

    [Fact]
    public async Task HistoryHidesRedactedContentAndLabelsEarlierVersionsAsync()
    {
        await using var context = new BunitContext();
        var original = Guid.NewGuid();
        var player = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        var notebook = new PlayerNotebook(player, false,
            [new(Guid.NewGuid(), player, "Do not render sensitive content", "Coach", time, null, false, time, "Administrator", "Inappropriate content"),
             new(Guid.NewGuid(), player, "Corrected observation", "Coach", time, original, false), new(original, player, "Earlier observation", "Coach", time, null, false)], [],
            [new(AttendanceKind.Absent, "Coach", time)]);
        var page = context.Render<PlayerHistory>(parameters => parameters.Add(value => value.Notebook, notebook));
        page.Markup.ShouldNotContain("Do not render sensitive content");
        page.Markup.ShouldContain("Private text removed");
        page.Markup.ShouldContain("Inappropriate content");
        page.Markup.ShouldContain("Earlier version");
        page.Markup.ShouldContain("Earlier observation");
        page.Markup.ShouldContain("Absent");
    }

    [Fact]
    public async Task PrintReloadRemovesStaleAttendanceAndSupportsRetryAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        gateway.GetAttendanceAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>()).Returns(new AttendanceSummary[] { new(data.Roster[0].Player.Id, AttendanceKind.Present, 1, "Coach", DateTimeOffset.UtcNow) });
        var page = context.Render<TryoutPrint>(parameters => parameters.Add(value => value.ClubId, _clubId).Add(value => value.TryoutId, _tryoutId));
        page.FindAll("tbody tr").Count.ShouldBe(2);
        page.Find("tbody").TextContent.ShouldContain("Present");
        gateway.GetAttendanceAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>()).Returns(Task.FromException<IReadOnlyList<AttendanceSummary>>(new HttpRequestException("Unavailable")));
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Refresh print list", StringComparison.Ordinal)).ClickAsync();
        page.FindAll(".attendance-sheet").ShouldBeEmpty();
        page.FindAll("button").ShouldNotContain(value => string.Equals(value.TextContent, "Print list", StringComparison.Ordinal));
        gateway.GetAttendanceAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>()).Returns(Array.Empty<AttendanceSummary>());
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Reload", StringComparison.Ordinal)).ClickAsync();
        page.Find("tbody").TextContent.ShouldNotContain("Present");
        page.FindAll("tbody tr").ShouldAllBe(value => value.TextContent.Contains("Not recorded", StringComparison.Ordinal));
    }
}
