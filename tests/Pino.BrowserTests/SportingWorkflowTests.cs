using System.Diagnostics.CodeAnalysis;
using Microsoft.Playwright;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed partial class SportingWorkflowTests
{
    private const string SkipReason = "Set PINO_BROWSER_URL, PINO_MAILPIT_URL and PINO_TEST_DATABASE for the local Aspire application.";
    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task ClubCanPrepareEvaluateAndRevisitAPersistedSeasonAsync()
    {
        await using var session = await BrowserSession.CreateAsync();
        var email = await session.RegisterAsync();
        await session.CompleteProfileAsync(throughUi: true);
        var clubId = await CreateClubAsync(session);
        var path = $"/api/clubs/{clubId}/sport";
        await AddPlayerAsync(session, clubId);
        await ImportPlayersAsync(session, clubId);
        var overview = await PrepareSeasonAsync(session, clubId);
        var tryout = overview.Tryouts.ShouldHaveSingleItem();
        await EvaluateAsync(session, clubId, tryout.Id);
        var detail = await session.GetAsync<TryoutDetail>($"{path}/tryouts/{tryout.Id}");
        detail.Notes.ShouldHaveSingleItem().Text.ShouldBe("Scans before receiving. Finds the far-side runner.");
        detail.Roster.ShouldHaveSingleItem().CurrentTeamId.ShouldBe(overview.Teams.Single().Id);
        detail.Tryout.Complete.ShouldBeTrue();
        await VerifyConflictAndHistoryAsync(session, path, detail, overview);
        await VerifyArchivesAndImportsAsync(session, path, overview);
        await VerifySeasonIndependenceAsync(session, path, overview);
        await CaptureCompletedWorkspaceAsync(session, clubId, tryout.Id);
        await VerifyImportCommitAcknowledgementAsync(session, clubId, email);
    }

    private static async Task<Guid> CreateClubAsync(BrowserSession session)
    {
        var page = session.Page;
        await page.GetByRole(AriaRole.Button, new() { Name = "Create a club", Exact = true }).ClickAsync();
        await page.Locator("h1:focus").WaitForAsync();
        await page.GetByLabel("Club name", new() { Exact = true }).FillAsync("Test · Northside FC");
        await page.GetByLabel("Sport", new() { Exact = true }).FillAsync("Soccer");
        await page.GetByLabel("City", new() { Exact = true }).FillAsync("Chicago");
        await page.GetByLabel("State", new() { Exact = true }).SelectOptionAsync("IL");
        await page.GetByRole(AriaRole.Button, new() { Name = "Create club", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Open club", Exact = true }).ClickAsync();
        var access = await session.GetAsync<AccessSnapshot>("/api/clubs/access");
        var id = access.Membership.ShouldNotBeNull().Club.Id;
        await page.GetByRole(AriaRole.Heading, new() { Name = "Test · Northside FC", Exact = true, Level = 1 }).WaitForAsync();
        await session.CaptureAsync("club-empty-desktop");
        return id;
    }

    private static async Task AddPlayerAsync(BrowserSession session, Guid clubId)
    {
        var page = session.Page;
        await page.GotoAsync($"/clubs/{clubId}/players/new");
        await page.GetByLabel("First name", new() { Exact = true }).FillAsync("Jordan");
        await page.GetByLabel("Last name", new() { Exact = true }).FillAsync("Rivera");
        await page.GetByLabel("Player reference", new() { Exact = true }).FillAsync("NS-001");
        await page.GetByLabel("High-school graduation year").FillAsync("2030");
        await page.GetByLabel("Position (optional)", new() { Exact = true }).FillAsync("Midfielder");
        await page.GetByLabel("Player photo (optional)", new() { Exact = true }).SetInputFilesAsync(new FilePayload { Name = "player.png", MimeType = "image/png", Buffer = BrowserSession.Photo() });
        await page.GetByText("player.png selected. Save the player to upload it.", new() { Exact = true }).WaitForAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Save player", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Heading, new() { Name = "Jordan Rivera", Exact = true }).WaitForAsync();
        await page.GetByRole(AriaRole.Heading, new() { Name = "Season placements" }).WaitForAsync();
    }

    private static async Task ImportPlayersAsync(BrowserSession session, Guid clubId)
    {
        await session.Page.GotoAsync($"/clubs/{clubId}/players/import");
        await session.Page.Locator("#import-file:enabled").WaitForAsync();
        const string Csv = "PlayerReference,FirstName,LastName,GraduationYear,Position,ContactEmail\nNS-002,Casey,Chen,2031,Goalkeeper,\nNS-003,Morgan,Brooks,2029,Defender,";
        await session.Page.GetByLabel("Player CSV", new() { Exact = true }).SetInputFilesAsync(new FilePayload { Name = "players.csv", MimeType = "text/csv", Buffer = System.Text.Encoding.UTF8.GetBytes(Csv) });
        await session.Page.GetByRole(AriaRole.Button, new() { Name = "Import 2 players", Exact = true }).ClickAsync();
        await session.Page.GetByRole(AriaRole.Link, new() { Name = "Open player catalog" }).WaitForAsync();
        var players = await session.GetAsync<PlayerPage>($"/api/clubs/{clubId}/sport/players?query=&archived=false&page=0");
        players.Players.Select(player => player.PlayerReference).Order(StringComparer.Ordinal).ShouldBe(["NS-001", "NS-002", "NS-003"]);
        await session.CaptureAsync("import-desktop");
    }

    private static async Task<SportOverview> PrepareSeasonAsync(BrowserSession session, Guid clubId)
    {
        var page = session.Page;
        await page.GotoAsync($"/clubs/{clubId}/seasons");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add season", Exact = true }).ClickAsync();
        await page.GetByLabel("Season name", new() { Exact = true }).FillAsync("Spring 2027");
        await page.GetByLabel("Start date", new() { Exact = true }).FillAsync("2027-01-01");
        await page.GetByLabel("End date", new() { Exact = true }).FillAsync("2027-06-30");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save season", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Heading, new() { Name = "Spring 2027", Exact = true }).WaitForAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Add team", Exact = true }).ClickAsync();
        await page.GetByLabel("Team name", new() { Exact = true }).FillAsync("Northside Blue");
        await page.GetByLabel("Earliest graduation year").FillAsync("2030");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save team", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Northside Blue", Exact = true }).WaitForAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Add tryout", Exact = true }).ClickAsync();
        await page.GetByLabel("Tryout name", new() { Exact = true }).FillAsync("Spring field evaluations");
        await page.GetByLabel("Tryout date", new() { Exact = true }).FillAsync("2027-02-20");
        await page.GetByLabel("Location (optional)", new() { Exact = true }).FillAsync("Lincoln Park · Field 3");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save tryout", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Spring field evaluations", Exact = true }).WaitForAsync();
        await session.CaptureAsync("season-desktop");
        return await session.GetAsync<SportOverview>($"/api/clubs/{clubId}/sport/overview");
    }

    private static async Task EvaluateAsync(BrowserSession session, Guid clubId, Guid tryoutId)
    {
        var page = session.Page;
        await page.GotoAsync($"/clubs/{clubId}/tryouts/{tryoutId}");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add players", Exact = true }).ClickAsync();
        await page.GetByLabel("Bib (optional, next player only)").FillAsync("17");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add Jordan Rivera to tryout", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Close enrollment", Exact = true }).ClickAsync();
        await page.GetByLabel("Add shared note", new() { Exact = true }).FillAsync("Scans before receiving. Finds the far-side runner.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save note", Exact = true }).ClickAsync();
        await page.Locator(".note-text").GetByText("Scans before receiving. Finds the far-side runner.", new() { Exact = true }).WaitForAsync();
        await page.Locator(".decision-editor summary").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        (await page.Locator(".decision-editor").GetAttributeAsync("open")).ShouldNotBeNull();
        await page.GetByLabel("Outcome", new() { Exact = true }).SelectOptionAsync("Placed");
        await page.GetByLabel("Eligible team", new() { Exact = true }).SelectOptionAsync(new SelectOptionValue { Label = "Northside Blue · 2030+" });
        await page.GetByLabel("Reason or revision context (optional)").FillAsync("Strong passing and field awareness.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save decision", Exact = true }).ClickAsync();
        await page.GetByText("All decisions recorded", new() { Exact = true }).WaitForAsync();
        await page.ReloadAsync();
        await page.Locator("#player-note:enabled").WaitForAsync();
        await page.Locator(".note-text").GetByText("Scans before receiving. Finds the far-side runner.", new() { Exact = true }).WaitForAsync();
        await session.CaptureAsync("tryout-desktop");
        await page.SetViewportSizeAsync(390, 844);
        await session.CaptureAsync("tryout-mobile-roster");
        await page.GetByRole(AriaRole.Button, new() { Name = "17 Jordan Rivera" }).ClickAsync();
        await session.CaptureAsync("tryout-mobile-notebook");
        (await page.Locator("h2:focus").InnerTextAsync()).ShouldBe("Jordan Rivera");
        await page.GetByRole(AriaRole.Button, new() { Name = "Back to roster", Exact = true }).ClickAsync();
        (await page.Locator("h2:focus").InnerTextAsync()).ShouldContain("Players");
        await page.SetViewportSizeAsync(1440, 1000);
    }
}
