using Bunit;
using NSubstitute;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Sporting;

public sealed partial class TryoutWorkTests
{
    [Fact]
    public async Task BibSaveUsesObservedValueAndPreservesOtherDraftsAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        gateway.SaveBibNumberAsync(_clubId, _tryoutId, Arg.Any<BibNumberInput>(), Arg.Any<CancellationToken>())
            .Returns(new SportReply(SportReplyKind.Saved, "Bib number saved."));
        var page = Render(context);
        await page.Find("#player-note").InputAsync("Keep this note.");
        await page.Find("#decision-reason").ChangeAsync("Keep this decision context.");
        await page.Find("#player-bib").InputAsync("41");
        gateway.GetTryoutAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>())
            .Returns(data with { Roster = [data.Roster[0] with { Bib = "41" }, data.Roster[1]] });
        await page.Find(".bib-editor").SubmitAsync();
        await gateway.Received(1).SaveBibNumberAsync(_clubId, _tryoutId,
            new(data.Roster[0].Player.Id, "41", "17"), Arg.Any<CancellationToken>());
        page.Find(".roster-record .bib-badge").GetAttribute("aria-label").ShouldBe("Bib 41");
        page.Find("#player-note").GetAttribute("value").ShouldBe("Keep this note.");
        page.Find("#decision-reason").GetAttribute("value").ShouldBe("Keep this decision context.");
        page.Find(".bib-editor button[type='submit']").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task SwitchingPlayersRetainsIndependentBibDraftsAndCanClearAssignmentAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        gateway.SaveBibNumberAsync(_clubId, _tryoutId, Arg.Any<BibNumberInput>(), Arg.Any<CancellationToken>())
            .Returns(new SportReply(SportReplyKind.Saved, "Bib number cleared."));
        var page = Render(context);
        await page.Find("#player-bib").InputAsync("41");
        await page.FindAll(".roster-record")[1].ClickAsync();
        page.Find("#player-bib").GetAttribute("value").ShouldBe("22");
        await page.Find("#player-bib").InputAsync("");
        await page.FindAll(".roster-record")[0].ClickAsync();
        page.Find("#player-bib").GetAttribute("value").ShouldBe("41");
        await page.FindAll(".roster-record")[1].ClickAsync();
        await page.Find(".bib-editor").SubmitAsync();
        await gateway.Received(1).SaveBibNumberAsync(_clubId, _tryoutId,
            new(data.Roster[1].Player.Id, "", "22"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BibConflictKeepsInputAndReloadRebasesOnlyBibAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        gateway.SaveBibNumberAsync(_clubId, _tryoutId, Arg.Any<BibNumberInput>(), Arg.Any<CancellationToken>())
            .Returns(new SportReply(SportReplyKind.Conflict, "This bib number changed."));
        var page = Render(context);
        await page.Find("#player-note").InputAsync("Keep this observation.");
        await page.Find("#player-bib").InputAsync("41");
        await page.Find(".bib-editor").SubmitAsync();
        page.Find("#player-bib").GetAttribute("value").ShouldBe("41");
        page.Find(".notice[data-kind='error']").TextContent.ShouldContain("bib number changed");
        gateway.GetTryoutAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>())
            .Returns(data with { Roster = [data.Roster[0] with { Bib = "50" }, data.Roster[1]] });
        await page.Find(".bib-editor button[type='button']").ClickAsync();
        page.Find("#player-bib").GetAttribute("value").ShouldBe("50");
        page.Find("#player-note").GetAttribute("value").ShouldBe("Keep this observation.");
        await page.Find("#player-bib").InputAsync("51");
        await page.Find(".bib-editor").SubmitAsync();
        await gateway.Received(1).SaveBibNumberAsync(_clubId, _tryoutId,
            new(data.Roster[0].Player.Id, "51", "50"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BibTransportFailurePreservesInputForRetryAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context, Data());
        gateway.SaveBibNumberAsync(_clubId, _tryoutId, Arg.Any<BibNumberInput>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SportReply>(new HttpRequestException("offline")));
        var page = Render(context);
        await page.Find("#player-bib").InputAsync("41");
        await page.Find(".bib-editor").SubmitAsync();
        page.Find("#player-bib").GetAttribute("value").ShouldBe("41");
        page.Find(".notice[data-kind='error']").TextContent.ShouldContain("unsaved input is still here");
    }
}
