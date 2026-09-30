using Microsoft.Playwright;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task DidNotAttendAndStaffNotesPersistThroughCloseoutAsync()
    {
        await using var session = await BrowserSession.CreateAsync();
        await session.RegisterAsync();
        await session.CompleteProfileAsync(throughUi: false);
        var data = await PrepareCloseoutAsync(session);
        var path = $"/api/clubs/{data.ClubId}/sport/tryouts/{data.Tryout.Id}";
        var notebookPath = $"{path}/players/{data.Player.Id}/notebook";
        (await session.Context.APIRequest.GetAsync(path + "/sessions")).Status.ShouldBe(404);
        (await session.Context.APIRequest.GetAsync(path + "/attendance")).Status.ShouldBe(404);
        var detail = await session.GetAsync<TryoutDetail>(path);
        var player = detail.Roster.Single(value => value.Player.Id == data.Player.Id);
        player.Decision.ShouldBe(DecisionKind.Placed);
        player.CurrentTeamId.ShouldBe(data.Blue.Id);
        player.Bib.ShouldBe("17");
        detail.Tryout.Decided.ShouldBe(3);
        detail.Roster.Count.ShouldBe(3);

        var page = session.Page;
        await page.GotoAsync($"/clubs/{data.ClubId}/tryouts/{data.Tryout.Id}");
        await page.Locator(".roster-record:enabled").First.WaitForAsync();
        (await page.Locator("#current-session, #note-session").CountAsync()).ShouldBe(0);
        await page.Locator(".roster-record").Filter(new() { HasText = "Jordan Rivera" }).ClickAsync();
        (await page.GetByRole(AriaRole.Heading, new() { Name = "Attendance", Exact = true }).CountAsync()).ShouldBe(0);
        await page.GetByLabel("Add shared note", new() { Exact = true }).FillAsync("Finds space after the first pass.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save note", Exact = true }).ClickAsync();
        await page.GetByText("Finds space after the first pass.", new() { Exact = true }).WaitForAsync();
        await page.WaitForFunctionAsync("() => [...document.querySelectorAll('.record-history .staff-avatar img')].some(image => image.complete && image.naturalWidth > 0)");
        await CapturePreparationAsync(session, "tryout-staff-photos");
        // A 720px CSS viewport checks the reflow of a 1440px screen at 200% zoom.
        await page.SetViewportSizeAsync(720, 500);
        await page.GetByLabel("Add shared note", new() { Exact = true }).FocusAsync();
        (await page.Locator(":focus").GetAttributeAsync("id")).ShouldBe("player-note");
        (await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth")).ShouldBeTrue();
        await page.EvaluateAsync("window.scrollTo(0, 0)");
        await session.CaptureAsync("tryout-staff-photos-200-percent-reflow");
        await page.SetViewportSizeAsync(1440, 1000);

        var notebook = await session.GetAsync<PlayerNotebook>(notebookPath);
        notebook.Notes.Count.ShouldBe(2);
        var photo = notebook.Notes[0].AuthorPhotoUrl.ShouldNotBeNull();
        notebook.Notes.ShouldAllBe(value => value.AuthorPhotoUrl == photo && value.Author == "Avery Coach");
        notebook.History.ShouldAllBe(value => value.AuthorPhotoUrl == photo);
        (await session.Context.APIRequest.GetAsync(photo.ToString())).Status.ShouldBe(200);
        await VerifyDidNotAttendCloseoutAsync(session, data, detail, photo);
    }

    private static async Task VerifyDidNotAttendCloseoutAsync(BrowserSession session, CloseoutFixture data, TryoutDetail detail, Uri photo)
    {
        var path = $"/api/clubs/{data.ClubId}/sport/tryouts/{data.Tryout.Id}";
        var other = detail.Roster.Single(value => value.Player.Id == data.Player.Id);
        (await session.PostAsync<SportReply>(path + "/decisions", new DecisionInput(Guid.NewGuid(), other.Player.Id, DecisionKind.DidNotAttend, null, other.Revision, other.PlacementRevision, "Explicit staff outcome"))).Kind.ShouldBe(SportReplyKind.Saved);
        var updated = await session.GetAsync<TryoutDetail>(path);
        var noPlacement = updated.Roster.Single(value => value.Player.Id == other.Player.Id);
        noPlacement.Decision.ShouldBe(DecisionKind.DidNotAttend);
        noPlacement.CurrentTeamId.ShouldBeNull();
        updated.Tryout.Complete.ShouldBeTrue();
        var review = await session.GetAsync<TryoutReview>(path + "/review");
        review.Results.Single(value => value.PlayerId == other.Player.Id).Decision.ShouldBe(DecisionKind.DidNotAttend);
        var editionId = Guid.NewGuid();
        (await session.PostAsync<SportReply>(path + "/close", new CloseTryoutInput(editionId, review.ReviewToken))).Kind.ShouldBe(SportReplyKind.Saved);
        var closed = (await session.GetAsync<TryoutReview>(path + "/review")).Closeouts.ShouldHaveSingleItem();
        closed.Results.Count.ShouldBe(3);
        closed.Results.Single(value => value.PlayerId == other.Player.Id).Decision.ShouldBe(DecisionKind.DidNotAttend);
        (await session.PostAsync<SportReply>(path + "/decisions", new DecisionInput(Guid.NewGuid(), other.Player.Id, DecisionKind.Withdrawn, null, noPlacement.Revision, noPlacement.PlacementRevision, "Closed"))).Kind.ShouldBe(SportReplyKind.Invalid);
        var csv = await (await session.Context.APIRequest.GetAsync(path + "/results.csv")).TextAsync();
        csv.ShouldContain("Did not attend");
        closed.ClosedByPhotoUrl.ShouldBe(photo);
        (await session.PostAsync<SportReply>(path + "/reopen", new ReopenTryoutInput(editionId, "Review a final decision"))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.GetAsync<TryoutReview>(path + "/review")).Closeouts.ShouldHaveSingleItem().ReopenedByPhotoUrl.ShouldBe(photo);
    }
}
