using Bunit;
using NSubstitute;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Sporting;

public sealed partial class TryoutWorkTests
{
    [Fact]
    public async Task PlayerSwitchLoadsOnlyThatNotebookAndClearsPreviousObservationsWhileWaitingAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        var first = data.Roster[0].Player.Id;
        var second = data.Roster[1].Player.Id;
        gateway.GetNotebookAsync(_clubId, _tryoutId, first, Arg.Any<CancellationToken>()).Returns(new PlayerNotebook(first, false,
            [new(Guid.NewGuid(), first, "Jordan's saved observation", "Coach", DateTimeOffset.UtcNow, null, false)], [], []));
        var pending = new TaskCompletionSource<PlayerNotebook>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.GetNotebookAsync(_clubId, _tryoutId, second, Arg.Any<CancellationToken>()).Returns(pending.Task);
        var page = Render(context);
        page.Markup.ShouldContain("Jordan's saved observation");
        await gateway.DidNotReceive().GetNotebookAsync(_clubId, _tryoutId, second, Arg.Any<CancellationToken>());
        var selection = page.FindAll(".roster-record")[1].ClickAsync();
        await page.WaitForAssertionAsync(() =>
        {
            page.Find(".notebook-heading h2").TextContent.ShouldBe("Casey Chen");
            page.Markup.ShouldNotContain("Jordan's saved observation");
            page.Find(".note-composer fieldset").HasAttribute("disabled").ShouldBeTrue();
        });
        pending.SetResult(new(second, false, [new(Guid.NewGuid(), second, "Casey's observation", "Coach", DateTimeOffset.UtcNow, null, false)], [], []));
        await selection;
        page.Markup.ShouldContain("Casey's observation");
        await gateway.Received(1).GetTryoutAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SavingNoteRefreshesSelectedNotebookWithoutReloadingWholeRosterAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        gateway.AddNoteAsync(_clubId, _tryoutId, Arg.Any<NoteInput>(), Arg.Any<CancellationToken>()).Returns(new SportReply(SportReplyKind.Saved, "Shared note saved."));
        var page = Render(context);
        await page.Find("#player-note").InputAsync("A new observation.");
        await page.Find(".note-composer").SubmitAsync();
        await gateway.Received(1).GetTryoutAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>());
        await gateway.Received(2).GetNotebookAsync(_clubId, _tryoutId, data.Roster[0].Player.Id, Arg.Any<CancellationToken>());
        page.Find("#player-note").GetAttribute("value").ShouldBeEmpty();
    }
}
