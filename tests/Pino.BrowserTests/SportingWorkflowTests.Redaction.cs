using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Pino.Data;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task RedactionRemovesEveryVersionInClosedArchivedTryoutsAndRejectsStaleCorrectionsAsync()
    {
        await using var session = await BrowserSession.CreateAsync();
        await session.RegisterAsync();
        await session.CompleteProfileAsync(throughUi: false);
        var data = await PrepareCloseoutAsync(session);
        var path = $"/api/clubs/{data.ClubId}/sport";
        var tryoutPath = $"{path}/tryouts/{data.Tryout.Id}";
        var notebookPath = $"{tryoutPath}/players/{data.Player.Id}/notebook";
        var correction = new NoteInput(Guid.NewGuid(), data.Player.Id, "Synthetic sensitive correction for redaction test.", data.Note.Id);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", correction)).Kind.ShouldBe(SportReplyKind.Saved);
        var unrelated = new NoteInput(Guid.NewGuid(), data.Player.Id, "Keep this independent observation.");
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", unrelated)).Kind.ShouldBe(SportReplyKind.Saved);
        var review = await session.GetAsync<TryoutReview>(tryoutPath + "/review");
        (await session.PostAsync<SportReply>(tryoutPath + "/close", new CloseTryoutInput(Guid.NewGuid(), review.ReviewToken))).Kind.ShouldBe(SportReplyKind.Saved);
        data.Season.Archived = true;
        data.Season.Revision = 1;
        (await session.PostAsync<SportReply>(path + "/seasons", data.Season)).Kind.ShouldBe(SportReplyKind.Saved);
        var input = new RedactNoteInput(Guid.NewGuid(), correction.Id, "Personal content inappropriate for shared notes", true);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes/redact", input with { Reason = " " })).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes/redact", input with { ConfirmAllVersions = false })).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.GetAsync<PlayerNotebook>(notebookPath)).Notes.Single(note => note.Id == correction.Id).Text.ShouldBe(correction.Text);

        var page = session.Page;
        await page.GotoAsync($"/clubs/{data.ClubId}/tryouts/{data.Tryout.Id}");
        await page.Locator(".roster-record:not([disabled])").First.WaitForAsync();
        await page.Locator(".roster-record").Filter(new() { HasText = "Jordan Rivera" }).ClickAsync();
        var observation = page.Locator(".record-history > li").Filter(new() { HasText = correction.Text });
        await observation.GetByRole(AriaRole.Button, new() { Name = "Review text removal", Exact = true }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.GetByLabel("Reason for removing text", new() { Exact = true }).FillAsync(input.Reason);
        (await page.GetByRole(AriaRole.Button, new() { Name = "Permanently remove text", Exact = true }).IsDisabledAsync()).ShouldBeTrue();
        await page.GetByLabel("I understand that this permanently removes the content from every version.", new() { Exact = true }).CheckAsync();
        await session.CaptureAsync("note-redaction-desktop");
        await page.SetViewportSizeAsync(390, 844);
        (await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth")).ShouldBeTrue();
        await session.CaptureAsync("note-redaction-mobile");
        await page.GetByRole(AriaRole.Button, new() { Name = "Permanently remove text", Exact = true }).ClickAsync();
        await page.GetByText("Note text removed from every version. The reason, administrator and time remain in history.", new() { Exact = true }).WaitForAsync();
        await session.CaptureAsync("note-redacted-mobile");
        var after = await session.GetAsync<TryoutDetail>(tryoutPath);
        after.Tryout.Closed.ShouldBeTrue();
        after.Season.Archived.ShouldBeTrue();
        var notebook = await session.GetAsync<PlayerNotebook>(notebookPath);
        var removed = notebook.Notes.Where(note => note.Id != unrelated.Id).ToArray();
        removed.Length.ShouldBe(2);
        foreach (var note in removed)
        {
            note.Text.ShouldBeEmpty();
            note.CanCorrect.ShouldBeFalse();
            note.RedactedAt.ShouldNotBeNull();
            note.RedactedBy.ShouldBe("Avery Coach");
            note.RedactionReason.ShouldBe(input.Reason);
        }
        notebook.Notes.Single(note => note.Id == unrelated.Id).Text.ShouldBe(unrelated.Text);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", correction)).Kind.ShouldBe(SportReplyKind.Conflict);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(Environment.GetEnvironmentVariable("PINO_TEST_DATABASE")).Options;
        await using var db = new ApplicationDbContext(options);
        var ct = TestContext.Current.CancellationToken;
        var stored = await db.PlayerNotes.AsNoTracking().Where(note => note.ClubId == data.ClubId && note.Id != unrelated.Id).ToListAsync(ct);
        stored.Count.ShouldBe(2);
        stored.ShouldAllBe(note => note.Text == "" && note.RedactedById != null && note.RedactionOperationId != null);
        var redactedAt = stored[0].RedactedAt;
        // An acknowledgement retry cannot replace who redacted the note or its original reason.
        (await session.PostAsync<SportReply>(tryoutPath + "/notes/redact", input)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.GetAsync<PlayerNotebook>(notebookPath)).Notes.Single(note => note.Id == correction.Id).RedactedAt.ShouldBe(redactedAt);
        data.Season.Archived = false;
        data.Season.Revision = 2;
        (await session.PostAsync<SportReply>(path + "/seasons", data.Season)).Kind.ShouldBe(SportReplyKind.Saved);
        var edition = (await session.GetAsync<TryoutReview>(tryoutPath + "/review")).Closeouts.ShouldHaveSingleItem();
        (await session.PostAsync<SportReply>(tryoutPath + "/reopen", new ReopenTryoutInput(edition.Id, "Verify stale correction rejection"))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", new NoteInput(Guid.NewGuid(), data.Player.Id, "Stale correction must not restore content", correction.Id))).Kind.ShouldBe(SportReplyKind.Conflict);
    }
}
