using Microsoft.Playwright;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task EnrollmentCorrectionsPreserveWorkAndEditionsWhileExcludingRemovedPlayersAsync()
    {
        await using var session = await BrowserSession.CreateAsync();
        await session.RegisterAsync();
        await session.CompleteProfileAsync(throughUi: false);
        var data = await PrepareCloseoutAsync(session);
        var path = $"/api/clubs/{data.ClubId}/sport";
        var tryoutPath = $"{path}/tryouts/{data.Tryout.Id}";
        var before = await session.GetAsync<TryoutDetail>(tryoutPath);
        var entry = before.Roster.Single(value => value.Player.Id == data.Player.Id);
        var removal = new EnrollmentChangeInput(Guid.NewGuid(), data.Player.Id, true, "Enrolled in this tryout by mistake.", entry.Revision, entry.PlacementRevision);
        (await session.PostAsync<SportReply>(tryoutPath + "/enrollments", removal with { Reason = " " })).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/enrollments", removal with { Revision = 0 })).Kind.ShouldBe(SportReplyKind.Conflict);
        var review = await session.GetAsync<TryoutReview>(tryoutPath + "/review");
        var firstEdition = Guid.NewGuid();
        (await session.PostAsync<SportReply>(tryoutPath + "/close", new CloseTryoutInput(firstEdition, review.ReviewToken))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/enrollments", removal)).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/reopen", new ReopenTryoutInput(firstEdition, "Correct enrollment"))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/attendance", new AttendanceInput(data.Player.Id, AttendanceKind.Present, 0))).Kind.ShouldBe(SportReplyKind.Saved);

        var page = session.Page;
        await page.GotoAsync($"/clubs/{data.ClubId}/tryouts/{data.Tryout.Id}/enrollments");
        await page.GetByRole(AriaRole.Button, new() { Name = "Review player for Jordan Rivera", Exact = true }).ClickAsync();
        await page.Locator("#correction-player:focus").WaitForAsync();
        await page.GetByLabel("Reason for exclusion", new() { Exact = true }).FillAsync(removal.Reason);
        await session.CaptureAsync("enrollment-removal-desktop");
        await page.SetViewportSizeAsync(390, 844);
        await session.CaptureAsync("enrollment-removal-mobile");
        await page.GetByRole(AriaRole.Button, new() { Name = "Exclude from this tryout", Exact = true }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.GetByText("Player excluded. Earlier work stays in history. This player no longer needs a decision to close the tryout.", new() { Exact = true }).WaitForAsync();
        await session.CaptureAsync("enrollment-removed-mobile");
        var removed = (await session.GetAsync<EnrollmentDetail[]>(tryoutPath + "/enrollments")).Single(value => value.Entry.Player.Id == data.Player.Id);
        removed.Removed.ShouldBeTrue();
        removed.Entry.CurrentTeamId.ShouldBeNull();
        var recorded = removed.History.ShouldHaveSingleItem();
        recorded.Reason.ShouldBe(removal.Reason);
        recorded.Author.ShouldBe("Avery Coach");
        removal = removal with { OperationId = recorded.Id };
        (await session.PostAsync<SportReply>(tryoutPath + "/enrollments", removal)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/enrollments", removal with { Reason = "Changed reason" })).Kind.ShouldBe(SportReplyKind.Conflict);
        await VerifyRemovedEnrollmentAsync(session, data, before);

        var current = await session.GetAsync<TryoutReview>(tryoutPath + "/review");
        current.Results.Count.ShouldBe(2);
        current.Closeouts.ShouldHaveSingleItem().Results.Count.ShouldBe(3);
        var secondEdition = Guid.NewGuid();
        (await session.PostAsync<SportReply>(tryoutPath + "/close", new CloseTryoutInput(secondEdition, current.ReviewToken))).Kind.ShouldBe(SportReplyKind.Saved);
        var restore = new EnrollmentChangeInput(Guid.NewGuid(), data.Player.Id, false, "Verified correct tryout after all.", removed.Entry.Revision, removed.Entry.PlacementRevision, "17");
        (await session.PostAsync<SportReply>(tryoutPath + "/enrollments", restore)).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/reopen", new ReopenTryoutInput(secondEdition, "Restore mistaken removal"))).Kind.ShouldBe(SportReplyKind.Saved);
        var other = before.Roster.First(value => value.Player.Id != data.Player.Id);
        (await session.PostAsync<SportReply>(tryoutPath + "/bib", new BibNumberInput(other.Player.Id, "17", other.Bib))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/enrollments", restore)).Kind.ShouldBe(SportReplyKind.Invalid);
        await page.ReloadAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Select all matching", Exact = true }).And(page.Locator(":enabled")).WaitForAsync();
        await page.GetByLabel("Player status", new() { Exact = true }).SelectOptionAsync("removed");
        await page.GetByRole(AriaRole.Button, new() { Name = "Review player for Jordan Rivera", Exact = true }).ClickAsync();
        await page.GetByLabel("Reason for restoration", new() { Exact = true }).FillAsync(restore.Reason);
        await page.GetByLabel("Bib number when restored (optional)", new() { Exact = true }).FillAsync("28");
        await page.SetViewportSizeAsync(1440, 1000);
        await session.CaptureAsync("enrollment-restoration-desktop");
        await page.GetByRole(AriaRole.Button, new() { Name = "Restore player", Exact = true }).ClickAsync();
        await page.GetByText("Player restored. Earlier work stays in history. Save a new final decision before closing the tryout.", new() { Exact = true }).WaitForAsync();
        var restored = (await session.GetAsync<TryoutDetail>(tryoutPath)).Roster.Single(value => value.Player.Id == data.Player.Id);
        restored.Bib.ShouldBe("28");
        restored.Decision.ShouldBe(DecisionKind.Awaiting);
        restored.DecisionTeamId.ShouldBeNull();
        restored.CurrentTeamId.ShouldBeNull();
        var incomplete = await session.GetAsync<TryoutReview>(tryoutPath + "/review");
        incomplete.Closeouts.Count.ShouldBe(2);
        (await session.PostAsync<SportReply>(tryoutPath + "/close", new CloseTryoutInput(Guid.NewGuid(), incomplete.ReviewToken))).Kind.ShouldBe(SportReplyKind.Invalid);
        await VerifyOtherTryoutPlacementSurvivesRemovalAsync(session, data);
    }

    private static async Task VerifyRemovedEnrollmentAsync(BrowserSession session, CloseoutFixture data, TryoutDetail before)
    {
        var path = $"/api/clubs/{data.ClubId}/sport";
        var tryoutPath = $"{path}/tryouts/{data.Tryout.Id}";
        var current = await session.GetAsync<TryoutDetail>(tryoutPath);
        current.Tryout.Players.ShouldBe(2);
        current.Tryout.Decided.ShouldBe(2);
        current.RemovedPlayers.ShouldBe(1);
        current.Roster.ShouldNotContain(value => value.Player.Id == data.Player.Id);
        var notebook = await session.GetAsync<PlayerNotebook>($"{tryoutPath}/players/{data.Player.Id}/notebook");
        notebook.Removed.ShouldBeTrue();
        notebook.Notes.ShouldHaveSingleItem().Text.ShouldBe(data.Note.Text);
        notebook.History.ShouldHaveSingleItem().Kind.ShouldBe(DecisionKind.Placed);
        notebook.Attendance.ShouldHaveSingleItem().Kind.ShouldBe(AttendanceKind.Present);
        (await session.GetAsync<AttendanceSummary[]>($"{tryoutPath}/attendance")).ShouldBeEmpty();
        (await session.GetAsync<SeasonReview>($"{path}/seasons/{data.Season.Id}/review")).Players.ShouldBe(2);
        var old = before.Roster.Single(value => value.Player.Id == data.Player.Id);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", new NoteInput(Guid.NewGuid(), data.Player.Id, "Rejected on removed entry"))).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/attendance", new AttendanceInput(data.Player.Id, AttendanceKind.Absent, 1))).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/bib", new BibNumberInput(data.Player.Id, "99", "17"))).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/decisions", new DecisionInput(Guid.NewGuid(), data.Player.Id, DecisionKind.Withdrawn, null, old.Revision, old.PlacementRevision, "Rejected"))).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/players", new EnrollmentInput(data.Player.Id, ""))).Kind.ShouldBe(SportReplyKind.Conflict);
    }

    private static async Task VerifyOtherTryoutPlacementSurvivesRemovalAsync(BrowserSession session, CloseoutFixture data)
    {
        var path = $"/api/clubs/{data.ClubId}/sport";
        var tryoutPath = $"{path}/tryouts/{data.Tryout.Id}";
        var later = new TryoutInput { SeasonId = data.Season.Id, Name = "Follow-up placement", Date = data.Tryout.Date.AddDays(7) };
        (await session.PostAsync<SportReply>(path + "/tryouts", later)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>($"{path}/tryouts/{later.Id}/bib", new BibNumberInput(data.Player.Id, "17", ""))).Kind.ShouldBe(SportReplyKind.Saved);
        await PlaceAsync(session, path, later.Id, data.Player.Id, data.Silver.Id);
        var entry = (await session.GetAsync<TryoutDetail>(tryoutPath)).Roster.Single(value => value.Player.Id == data.Player.Id);
        var request = new EnrollmentChangeInput(Guid.NewGuid(), data.Player.Id, true, "Follow-up tryout is correct.", entry.Revision, entry.PlacementRevision);
        var competing = await Task.WhenAll(session.PostAsync<SportReply>(tryoutPath + "/enrollments", request),
            session.PostAsync<SportReply>(tryoutPath + "/enrollments", request with { OperationId = Guid.NewGuid() }));
        competing.Select(value => value.Kind).ShouldBe([SportReplyKind.Saved, SportReplyKind.Conflict], ignoreOrder: true);
        (await session.GetAsync<TeamDetail>($"{path}/teams/{data.Silver.Id}")).Members.ShouldHaveSingleItem().Id.ShouldBe(data.Player.Id);
        var enrollment = (await session.GetAsync<EnrollmentDetail[]>(tryoutPath + "/enrollments")).Single(value => value.Entry.Player.Id == data.Player.Id);
        enrollment.History.Count.ShouldBe(3);
        enrollment.Entry.CurrentTryoutId.ShouldBe(later.Id);
    }
}
