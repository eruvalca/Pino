using Microsoft.Playwright;
using Pino.SharedKernel.Sporting;
using Shouldly;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    private static async Task VerifyCloseoutReopeningAsync(BrowserSession session, CloseoutFixture data, TryoutCloseoutSummary edition)
    {
        var path = $"/api/clubs/{data.ClubId}/sport";
        var tryoutPath = $"{path}/tryouts/{data.Tryout.Id}";
        (await session.PostAsync<SportReply>(tryoutPath + "/reopen", new ReopenTryoutInput(edition.Id, " "))).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/reopen", new ReopenTryoutInput(Guid.NewGuid(), "Correction"))).Kind.ShouldBe(SportReplyKind.Conflict);
        await session.Page.GotoAsync($"/clubs/{data.ClubId}/tryouts/{data.Tryout.Id}/review");
        await session.Page.Locator("button.text-action:enabled").WaitForAsync();
        await session.Page.GetByText("Reopen for a correction", new() { Exact = true }).ClickAsync();
        await session.Page.GetByLabel("Reason for reopening", new() { Exact = true }).FillAsync("Record the follow-up outcome.");
        await session.Page.GetByRole(AriaRole.Button, new() { Name = "Reopen tryout", Exact = true }).ClickAsync();
        await session.Page.Locator(".sport-heading .decision-badge[data-kind='awaiting']").WaitForAsync();
        (await session.Page.Locator(".sport-heading .decision-badge").InnerTextAsync()).ShouldBe("Open");
        var reopened = await session.GetAsync<TryoutReview>(tryoutPath + "/review");
        reopened.Tryout.Closed.ShouldBeFalse();
        reopened.Closeouts.ShouldHaveSingleItem().ReopenReason.ShouldBe("Record the follow-up outcome.");
        reopened.Closeouts[0].ReopenedBy.ShouldBe("Avery Coach");
        reopened.Closeouts[0].ReopenedAt.ShouldNotBeNull();
        (await session.PostAsync<SportReply>(tryoutPath + "/reopen", new ReopenTryoutInput(edition.Id, "Record the follow-up outcome."))).Kind.ShouldBe(SportReplyKind.Saved);
        var entry = (await session.GetAsync<TryoutDetail>(tryoutPath)).Roster.Single(value => value.Player.Id == data.Player.Id);
        (await session.PostAsync<SportReply>(tryoutPath + "/decisions", new DecisionInput(Guid.NewGuid(), data.Player.Id, DecisionKind.NotSelected, null, entry.Revision, entry.PlacementRevision, "Follow-up tryout now owns placement."))).Kind.ShouldBe(SportReplyKind.Saved);
        var current = await session.GetAsync<TryoutReview>(tryoutPath + "/review");
        var competing = await Task.WhenAll(
            session.PostAsync<SportReply>(tryoutPath + "/close", new CloseTryoutInput(Guid.NewGuid(), current.ReviewToken)),
            session.PostAsync<SportReply>(tryoutPath + "/close", new CloseTryoutInput(Guid.NewGuid(), current.ReviewToken)));
        competing.Count(value => value.Kind == SportReplyKind.Saved).ShouldBe(1);
        competing.Count(value => value.Kind == SportReplyKind.Conflict).ShouldBe(1);
        var closedAgain = await session.GetAsync<TryoutReview>(tryoutPath + "/review");
        closedAgain.Closeouts.Count.ShouldBe(2);
        closedAgain.Closeouts.Single(value => value.Id == edition.Id).Results.Single(value => value.PlayerId == data.Player.Id).Decision.ShouldBe(DecisionKind.Placed);
        closedAgain.Closeouts.Single(value => value.Id != edition.Id).Results.Single(value => value.PlayerId == data.Player.Id).Decision.ShouldBe(DecisionKind.NotSelected);
        (await session.PostAsync<SportReply>(tryoutPath + "/reopen", new ReopenTryoutInput(edition.Id, "Stale edition"))).Kind.ShouldBe(SportReplyKind.Conflict);
        (await session.GetAsync<SeasonReview>($"{path}/seasons/{data.Season.Id}/review")).Teams.Single(value => value.Team.Id == data.Silver.Id).Players.ShouldHaveSingleItem().Id.ShouldBe(data.Player.Id);
        await session.Page.ReloadAsync();
        await session.Page.Locator("button.text-action:enabled").WaitForAsync();
        await session.Page.GetByLabel("Saved results", new() { Exact = true }).SelectOptionAsync(edition.Id.ToString());
        (await session.Page.Locator(".result-ledger").InnerTextAsync()).ShouldContain("Jordan Rivera");
        (await session.Page.Locator(".result-ledger").InnerTextAsync()).ShouldNotContain("Jordan Updated");
    }

    private static async Task CaptureReviewAsync(BrowserSession session, string name)
    {
        foreach (var (width, height, suffix) in new[] { (1440, 1000, "desktop"), (390, 844, "mobile"), (320, 800, "narrow") })
        {
            await session.Page.SetViewportSizeAsync(width, height);
            await session.Page.EvaluateAsync("window.scrollTo(0, 0)");
            await session.Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
            (await session.Page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth")).ShouldBeTrue($"No horizontal overflow at {width}px.");
            await session.CaptureAsync($"{name}-{suffix}");
        }
        await session.Page.SetViewportSizeAsync(1440, 1000);
        await session.Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference });
    }
}
