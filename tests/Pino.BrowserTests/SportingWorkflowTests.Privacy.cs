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
    public async Task ArchivedImportResolutionPreservesOrErasesTheWholePlayerHistoryAsync()
    {
        await using var session = await BrowserSession.CreateAsync();
        await session.RegisterAsync();
        await session.CompleteProfileAsync(throughUi: false);
        var data = await PrepareCloseoutAsync(session);
        var path = $"/api/clubs/{data.ClubId}/sport";
        var tryoutPath = $"{path}/tryouts/{data.Tryout.Id}";
        (await session.PostAsync<SportReply>(tryoutPath + "/attendance", new AttendanceInput(data.Player.Id, AttendanceKind.Present, 0))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", new NoteInput(Guid.NewGuid(), data.Player.Id, "An earlier version must also be erased.", data.Note.Id))).Kind.ShouldBe(SportReplyKind.Saved);
        await PrepareEnrollmentHistoryForErasureAsync(session, data);
        var review = await session.GetAsync<TryoutReview>(tryoutPath + "/review");
        (await session.PostAsync<SportReply>(tryoutPath + "/close", new CloseTryoutInput(Guid.NewGuid(), review.ReviewToken))).Kind.ShouldBe(SportReplyKind.Saved);
        data.Player.Archived = true;
        data.Player.Revision = 1;
        data.Player.Position = "Original position";
        data.Player.Photo = Convert.ToBase64String(BrowserSession.Photo());
        (await session.PostAsync<SportReply>(path + "/players", data.Player)).Kind.ShouldBe(SportReplyKind.Saved);
        data.Player.Photo = null;
        const string Csv = "PlayerReference,FirstName,LastName,GraduationYear,Position,ContactEmail\nRETURN-1,Jordan,Rivera,2030,Changed by CSV,new@example.test";
        var unresolved = await session.PostAsync<ImportReport>(path + "/import", new ImportInput(Csv, Commit: true));
        unresolved.Saved.ShouldBeFalse();
        unresolved.Counts.ShouldNotBeNull().Unresolved.ShouldBe(1);
        var candidate = unresolved.Rows.ShouldHaveSingleItem().Candidates.ShouldNotBeNull().ShouldHaveSingleItem();
        var restore = new ImportInput(Csv, Commit: true, Resolutions: [new(2, ImportDisposition.Reactivate, candidate.Id, candidate.Revision)]);
        var restored = await session.PostAsync<ImportReport>(path + "/import", restore);
        restored.Counts.ShouldBe(new ImportCounts(0, 0, 1, 0, 0));
        var original = await session.GetAsync<PlayerDetail>($"{path}/players/{data.Player.Id}");
        original.Player.Archived.ShouldBeFalse();
        original.Player.Position.ShouldBe("Original position");
        original.Player.ContactEmail.ShouldBeEmpty();
        original.History.Count.ShouldBe(2);
        (await session.GetAsync<PlayerNotebook>($"{tryoutPath}/players/{data.Player.Id}/notebook")).Notes.Count.ShouldBe(2);
        (await session.GetAsync<TryoutReview>(tryoutPath + "/review")).Closeouts.ShouldHaveSingleItem().Results.Count.ShouldBe(3);
        data.Player.Revision = original.Player.Revision;
        (await session.PostAsync<SportReply>(path + "/players", data.Player)).Kind.ShouldBe(SportReplyKind.Saved);
        await ReplaceArchivedThroughReviewAsync(session, data.ClubId, data.Player.Id, Csv);
        var replacement = (await session.GetAsync<PlayerPage>(path + "/players?query=RETURN-1&archived=false&page=0")).Players.ShouldHaveSingleItem();
        replacement.Id.ShouldNotBe(data.Player.Id);
        replacement.Position.ShouldBe("Changed by CSV");
        (await session.Context.APIRequest.GetAsync($"{path}/players/{data.Player.Id}")).Status.ShouldBe(404);
        var after = await session.GetAsync<TryoutDetail>(tryoutPath);
        (await session.Context.APIRequest.GetAsync($"{tryoutPath}/players/{data.Player.Id}/notebook")).Status.ShouldBe(404);
        after.Roster.Count.ShouldBe(2);
        var edition = (await session.GetAsync<TryoutReview>(tryoutPath + "/review")).Closeouts.ShouldHaveSingleItem();
        edition.Results.Count.ShouldBe(2);
        edition.ErasedPlayers.ShouldBe(1);
        await VerifyErasureStorageAsync(data.ClubId, data.Player.Id, session);
        await EraseReplacementThroughUiAsync(session, data.ClubId, replacement);
        // Replaying a committed import after erasure must never recreate that player.
        (await session.PostAsync<ImportReport>(path + "/import", restore)).Saved.ShouldBeTrue();
        (await session.GetAsync<PlayerPage>(path + "/players?query=Jordan&archived=false&page=0")).Players.ShouldBeEmpty();
    }

    private static async Task PrepareEnrollmentHistoryForErasureAsync(BrowserSession session, CloseoutFixture data)
    {
        var path = $"/api/clubs/{data.ClubId}/sport";
        var tryoutPath = $"{path}/tryouts/{data.Tryout.Id}";
        var entry = (await session.GetAsync<TryoutDetail>(tryoutPath)).Roster.Single(value => value.Player.Id == data.Player.Id);
        (await session.PostAsync<SportReply>(tryoutPath + "/enrollments", new EnrollmentChangeInput(Guid.NewGuid(), data.Player.Id, true, "Synthetic correction before erasure", entry.Revision, entry.PlacementRevision))).Kind.ShouldBe(SportReplyKind.Saved);
        var removed = (await session.GetAsync<EnrollmentDetail[]>(tryoutPath + "/enrollments")).Single(value => value.Entry.Player.Id == data.Player.Id);
        (await session.PostAsync<SportReply>(tryoutPath + "/enrollments", new EnrollmentChangeInput(Guid.NewGuid(), data.Player.Id, false, "Restore synthetic enrollment", removed.Entry.Revision, removed.Entry.PlacementRevision, "17"))).Kind.ShouldBe(SportReplyKind.Saved);
        await PlaceAsync(session, path, data.Tryout.Id, data.Player.Id, data.Blue.Id);
    }

    private static async Task ReplaceArchivedThroughReviewAsync(BrowserSession session, Guid clubId, Guid playerId, string csv)
    {
        var page = session.Page;
        await page.GotoAsync($"/clubs/{clubId}/players/import");
        await page.Locator("#import-file:enabled").WaitForAsync();
        await page.GetByLabel("Player CSV", new() { Exact = true }).SetInputFilesAsync(new FilePayload { Name = "returning.csv", MimeType = "text/csv", Buffer = System.Text.Encoding.UTF8.GetBytes(csv) });
        await page.GetByLabel("Action for row 2", new() { Exact = true }).SelectOptionAsync("Replace");
        await page.GetByLabel("Archived player to review", new() { Exact = true }).SelectOptionAsync(playerId.ToString());
        await page.GetByLabel("Type Jordan Rivera to confirm", new() { Exact = true }).FillAsync("Jordan Rivera");
        await page.GetByLabel("I understand that this permanently removes this person's records and history.", new() { Exact = true }).CheckAsync();
        (await page.GetByRole(AriaRole.Button, new() { Name = "Import reviewed players", Exact = true }).IsDisabledAsync()).ShouldBeTrue();
        await page.GetByRole(AriaRole.Button, new() { Name = "Review updated choices", Exact = true }).ClickAsync();
        await page.Locator(".primary-action:not([disabled])").WaitForAsync();
        await session.CaptureAsync("import-replacement-desktop");
        await page.SetViewportSizeAsync(390, 844);
        (await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth")).ShouldBeTrue();
        await session.CaptureAsync("import-replacement-mobile");
        await page.SetViewportSizeAsync(1440, 1000);
        await page.GetByRole(AriaRole.Button, new() { Name = "Import reviewed players", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Open players", Exact = true }).WaitForAsync();
        (await page.Locator("main").InnerTextAsync()).ShouldContain("Created 1; skipped 0; reactivated 0.");
    }

    private static async Task VerifyErasureStorageAsync(Guid clubId, Guid oldPlayerId, BrowserSession session)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(Environment.GetEnvironmentVariable("PINO_TEST_DATABASE")).Options;
        await using var db = new ApplicationDbContext(options);
        var ct = TestContext.Current.CancellationToken;
        (await db.Players.AnyAsync(value => value.Id == oldPlayerId, ct)).ShouldBeFalse();
        (await db.PlayerNotes.AnyAsync(value => value.PlayerId == oldPlayerId, ct)).ShouldBeFalse();
        (await db.TryoutAttendances.AnyAsync(value => value.PlayerId == oldPlayerId, ct)).ShouldBeFalse();
        (await db.EnrollmentChanges.AnyAsync(value => value.PlayerId == oldPlayerId, ct)).ShouldBeFalse();
        (await db.DecisionEvents.AnyAsync(value => value.PlayerId == oldPlayerId, ct)).ShouldBeFalse();
        (await db.SeasonPlacements.AnyAsync(value => value.PlayerId == oldPlayerId, ct)).ShouldBeFalse();
        (await db.TryoutCloseoutPlayers.AnyAsync(value => value.PlayerId == oldPlayerId, ct)).ShouldBeFalse();
        var receipt = await db.PlayerErasures.AsNoTracking().SingleAsync(value => value.ClubId == clubId, ct);
        for (var attempt = 0; attempt < 80; attempt++)
        {
            var status = await session.GetAsync<ErasureReport>($"/api/clubs/{clubId}/sport/erasures/{receipt.Id}");
            if (!status.PhotoPending)
            {
                (await db.PlayerErasures.AsNoTracking().SingleAsync(value => value.Id == receipt.Id, ct)).PhotoKey.ShouldBeNull();
                return;
            }
            await Task.Delay(1000, ct);
        }
        throw new TimeoutException("Photo erasure did not complete within the cleanup interval.");
    }

    private static async Task EraseReplacementThroughUiAsync(BrowserSession session, Guid clubId, PlayerSummary player)
    {
        var path = $"/api/clubs/{clubId}/sport";
        var rejected = await session.PostAsync<ErasureReport>(path + "/players/erase", new ErasePlayerInput(Guid.NewGuid(), player.Id, player.Revision, player.FullName, false));
        rejected.Kind.ShouldBe(SportReplyKind.Invalid);
        var stale = await session.PostAsync<ErasureReport>(path + "/players/erase", new ErasePlayerInput(Guid.NewGuid(), player.Id, 0, player.FullName, true));
        stale.Kind.ShouldBe(SportReplyKind.Conflict);
        await session.Page.GotoAsync($"/clubs/{clubId}/players/{player.Id}/erase");
        await session.Page.Locator("#erase-player-name:enabled").WaitForAsync();
        await session.Page.GetByLabel($"Type {player.FullName} to confirm", new() { Exact = true }).FillAsync(player.FullName);
        (await session.Page.GetByRole(AriaRole.Button, new() { Name = "Permanently delete player", Exact = true }).IsDisabledAsync()).ShouldBeTrue();
        await session.Page.GetByLabel("I understand that this permanently removes this person's records and history.", new() { Exact = true }).CheckAsync();
        await session.CaptureAsync("player-erasure-desktop");
        await session.Page.GetByRole(AriaRole.Button, new() { Name = "Permanently delete player", Exact = true }).ClickAsync();
        await session.Page.WaitForURLAsync("**/erasures/*");
        await session.Page.ReloadAsync();
        await session.Page.GetByText("Player records, history and photos are deleted. Only the administrator who deleted them and the time are kept.", new() { Exact = true }).WaitForAsync();
    }
}
