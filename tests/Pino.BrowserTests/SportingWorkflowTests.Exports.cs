using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using NSubstitute;
using Pino.Data;
using Pino.Features.Clubs.Services;
using Pino.Features.Sporting.Services;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task ExportsAndPlayerHistoryPreserveEditionsRedactionsAndEnrollmentContextAsync()
    {
        await using var session = await BrowserSession.CreateAsync();
        var email = await session.RegisterAsync();
        await session.CompleteProfileAsync(throughUi: false);
        var data = await PrepareCloseoutAsync(session);
        var path = $"/api/clubs/{data.ClubId}/sport";
        var tryoutPath = $"{path}/tryouts/{data.Tryout.Id}";
        data.Player.Revision = 1;
        data.Player.MiddleName = "Taylor";
        data.Player.ContactEmail = "private-player@example.test";
        data.Player.Photo = Convert.ToBase64String(BrowserSession.Photo());
        (await session.PostAsync<SportReply>(path + "/players", data.Player)).Kind.ShouldBe(SportReplyKind.Saved);
        await PrepareEnrollmentHistoryForErasureAsync(session, data);
        (await session.PostAsync<SportReply>(tryoutPath + "/attendance", new AttendanceInput(data.Player.Id, AttendanceKind.Present, 0))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/attendance", new AttendanceInput(data.Player.Id, AttendanceKind.Absent, 1))).Kind.ShouldBe(SportReplyKind.Saved);
        var corrected = new NoteInput(Guid.NewGuid(), data.Player.Id, "Corrected sporting observation", data.Note.Id);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", corrected)).Kind.ShouldBe(SportReplyKind.Saved);
        var sensitive = new NoteInput(Guid.NewGuid(), data.Player.Id, "Synthetic sensitive content to remove");
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", sensitive)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes/redact", new RedactNoteInput(Guid.NewGuid(), sensitive.Id, "Inappropriate observation", true))).Kind.ShouldBe(SportReplyKind.Saved);
        var review = await session.GetAsync<TryoutReview>(tryoutPath + "/review");
        var editionId = Guid.NewGuid();
        (await session.PostAsync<SportReply>(tryoutPath + "/close", new CloseTryoutInput(editionId, review.ReviewToken))).Kind.ShouldBe(SportReplyKind.Saved);
        data.Player.Revision = 2;
        data.Player.FirstName = "Current Jordan";
        data.Player.Photo = null;
        (await session.PostAsync<SportReply>(path + "/players", data.Player)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/reopen", new ReopenTryoutInput(editionId, "Later placement correction"))).Kind.ShouldBe(SportReplyKind.Saved);
        await PlaceAsync(session, path, data.Tryout.Id, data.Player.Id, data.Silver.Id);
        var removed = (await session.GetAsync<TryoutDetail>(tryoutPath)).Roster.Single(value => string.Equals(value.Player.FirstName, "Avery", StringComparison.Ordinal));
        (await session.PostAsync<SportReply>(tryoutPath + "/enrollments", new EnrollmentChangeInput(Guid.NewGuid(), removed.Player.Id, true, "Enrolled in the wrong tryout", removed.Revision, removed.PlacementRevision))).Kind.ShouldBe(SportReplyKind.Saved);

        var recorded = await ReadCsvAsync(session, tryoutPath + $"/results.csv?editionId={editionId}");
        recorded.Count.ShouldBe(3);
        var original = recorded.Single(value => string.Equals(value["FirstName"], "Jordan", StringComparison.Ordinal));
        original["MiddleName"].ShouldBe("Taylor");
        original["DecisionTeam"].ShouldBe(data.Blue.Name);
        original["EditionId"].ShouldBe(editionId.ToString());
        original["RecordType"].ShouldBe("Recorded tryout edition");
        var current = await ReadCsvAsync(session, tryoutPath + "/results.csv");
        current.Count.ShouldBe(2);
        current.Single(value => string.Equals(value["FirstName"], "Current Jordan", StringComparison.Ordinal))["DecisionTeam"].ShouldBe(data.Silver.Name);
        current.ShouldAllBe(value => value["EditionId"].Length == 0 && string.Equals(value["RecordType"], "Current tryout outcomes", StringComparison.Ordinal));
        var team = await ReadCsvAsync(session, $"{path}/teams/{data.Silver.Id}/roster.csv?seasonId={data.Season.Id}");
        team.ShouldHaveSingleItem()["FirstName"].ShouldBe("Current Jordan");
        team[0].Keys.ShouldNotContain(value => string.Equals(value, "ContactEmail", StringComparison.Ordinal));
        (await ReadCsvAsync(session, $"{path}/teams/{data.Blue.Id}/roster.csv?seasonId={data.Season.Id}")).ShouldBeEmpty();
        var season = await ReadCsvAsync(session, $"{path}/seasons/{data.Season.Id}/roster.csv");
        season.Count.ShouldBe(2);
        season.ShouldContain(value => value["CurrentTeam"].Length == 0);
        (await session.Context.APIRequest.GetAsync(tryoutPath + $"/results.csv?editionId={Guid.NewGuid()}")).Status.ShouldBe(404);
        (await session.Context.APIRequest.GetAsync($"{path}/tryouts/{Guid.NewGuid()}/results.csv?editionId={editionId}")).Status.ShouldBe(404);
        await VerifyPersonalExportAsync(session, data, email, sensitive.Id);
        await VerifyHistoryAndExportUiAsync(session, data, editionId, removed.Player.Id);
        await VerifyAttendancePrintAsync(session, data);
        var player = (await session.GetAsync<PlayerDetail>($"{path}/players/{data.Player.Id}")).Player;
        (await session.PostAsync<ErasureReport>(path + "/players/erase", new ErasePlayerInput(Guid.NewGuid(), player.Id, player.Revision, player.FullName, true))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.Context.APIRequest.GetAsync($"{path}/players/{data.Player.Id}/personal-data")).Status.ShouldBe(404);
        (await ReadCsvAsync(session, tryoutPath + $"/results.csv?editionId={editionId}")).ShouldAllBe(value => !string.Equals(value["LastName"], "Rivera", StringComparison.Ordinal));
    }

    private static async Task<List<Dictionary<string, string>>> ReadCsvAsync(BrowserSession session, string path)
    {
        var response = await session.Context.APIRequest.GetAsync(path);
        response.Status.ShouldBe(200, await response.TextAsync());
        response.Headers["cache-control"].ShouldBe("no-store");
        response.Headers["content-disposition"].ShouldContain("attachment");
        var bytes = await response.BodyAsync();
        using var reader = new StreamReader(new MemoryStream(bytes), Encoding.UTF8);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        (await csv.ReadAsync()).ShouldBeTrue();
        csv.ReadHeader();
        var headers = csv.HeaderRecord.ShouldNotBeNull();
        headers.ShouldNotContain(value => string.Equals(value, "Notes", StringComparison.Ordinal));
        headers.ShouldNotContain(value => string.Equals(value, "ContactEmail", StringComparison.Ordinal));
        var records = new List<Dictionary<string, string>>();
        while (await csv.ReadAsync()) { records.Add(headers.ToDictionary(header => header, header => csv.GetField(header) ?? "", StringComparer.Ordinal)); }
        return records;
    }

    private static async Task VerifyPersonalExportAsync(BrowserSession session, CloseoutFixture data, string email, Guid redactedId)
    {
        var path = $"/api/clubs/{data.ClubId}/sport/players/{data.Player.Id}";
        var response = await session.Context.APIRequest.GetAsync(path + "/personal-data");
        response.Status.ShouldBe(200);
        response.Headers["cache-control"].ShouldBe("no-store");
        var content = await response.TextAsync();
        content.ShouldNotContain("Synthetic sensitive content to remove");
        content.ShouldNotContain("PhotoKey");
        content.ShouldNotContain("Morgan");
        using var document = JsonDocument.Parse(content);
        var package = document.RootElement.GetProperty("Data");
        package.GetProperty("Player").GetProperty("ContactEmail").GetString().ShouldBe("private-player@example.test");
        package.GetProperty("Notes").GetArrayLength().ShouldBe(3);
        package.GetProperty("Notes").EnumerateArray().Single(value => value.GetProperty("Note").GetProperty("Id").GetGuid() == redactedId).GetProperty("Note").GetProperty("Text").GetString().ShouldBeEmpty();
        package.GetProperty("Attendance").GetArrayLength().ShouldBe(1);
        package.GetProperty("EnrollmentChanges").GetArrayLength().ShouldBe(2);
        package.GetProperty("Placements").GetArrayLength().ShouldBe(1);
        package.GetProperty("Participation").GetArrayLength().ShouldBe(1);
        package.GetProperty("Decisions").GetArrayLength().ShouldBe(3);
        package.GetProperty("RecordedEditions").GetArrayLength().ShouldBe(1);
        package.GetProperty("RecordedEditions")[0].GetProperty("Result").GetProperty("FirstName").GetString().ShouldBe("Jordan");
        var photo = await session.Context.APIRequest.GetAsync(path + "/photo");
        Convert.FromBase64String(document.RootElement.GetProperty("PhotoJpegBase64").GetString().ShouldNotBeNull()).ShouldBe(await photo.BodyAsync());

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(Environment.GetEnvironmentVariable("PINO_TEST_DATABASE")).Options;
        var factory = new CloseoutContextFactory(options);
        await using var db = factory.CreateDbContext();
        var ct = TestContext.Current.CancellationToken;
        var id = await db.Users.Where(value => value.Email == email).Select(value => value.Id).SingleAsync(ct);
        var actor = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id)], "BrowserTest"));
        var photos = Substitute.For<IProfilePhotoStore>();
        photos.DownloadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<byte[]>(new IOException("Photo unavailable")));
        var service = new SportService(factory, photos, TimeProvider.System);
        (await Should.ThrowAsync<IOException>(() => service.ExportPlayerAsync(actor, data.ClubId, data.Player.Id, ct))).ShouldBeOfType<IOException>();
    }

    private static async Task VerifyHistoryAndExportUiAsync(BrowserSession session, CloseoutFixture data, Guid editionId, Guid removedPlayerId)
    {
        var page = session.Page;
        await page.GotoAsync($"/clubs/{data.ClubId}/players/{data.Player.Id}");
        await page.Locator("#history-tryout:enabled").WaitForAsync();
        await page.GetByLabel("Tryout to review", new() { Exact = true }).SelectOptionAsync(data.Tryout.Id.ToString());
        await page.Locator(".player-tryout-history").GetByText("Corrected sporting observation", new() { Exact = true }).WaitForAsync();
        (await page.Locator(".player-tryout-history").InnerTextAsync()).ShouldContain(data.Note.Text);
        (await page.Locator(".player-tryout-history").InnerTextAsync()).ShouldContain("Private text removed");
        (await page.Locator(".player-tryout-history").InnerTextAsync()).ShouldContain("Removed from active roster");
        await page.GetByLabel("Tryout to review", new() { Exact = true }).FocusAsync();
        await CapturePreparationAsync(session, "player-consolidated-history");
        await page.Locator("details summary").ClickAsync();
        var personalDownload = await page.RunAndWaitForDownloadAsync(() => page.GetByRole(AriaRole.Link, new() { Name = "Download player personal data (JSON)", Exact = true }).ClickAsync());
        personalDownload.SuggestedFilename.ShouldBe("Pino-player-personal-data.json");
        (await personalDownload.FailureAsync()).ShouldBeNull();
        await page.GotoAsync($"/clubs/{data.ClubId}/players/{removedPlayerId}");
        await page.Locator("#history-tryout:enabled").WaitForAsync();
        await page.GetByLabel("Tryout to review", new() { Exact = true }).SelectOptionAsync(data.Tryout.Id.ToString());
        await page.GetByText("Enrolled in the wrong tryout", new() { Exact = true }).WaitForAsync();
        await page.GotoAsync($"/clubs/{data.ClubId}/tryouts/{data.Tryout.Id}/review");
        await page.Locator(".sport-sheet button:enabled").First.WaitForAsync();
        await page.GetByLabel("Find a result", new() { Exact = true }).FillAsync("no matching player");
        await page.GetByText("0 of 2 results shown", new() { Exact = true }).WaitForAsync();
        var currentDownload = await page.RunAndWaitForDownloadAsync(() => page.GetByRole(AriaRole.Link, new() { Name = "Download current results (CSV)", Exact = true }).ClickAsync());
        currentDownload.SuggestedFilename.ShouldBe("Pino-current-tryout-results.csv");
        (await File.ReadAllTextAsync((await currentDownload.PathAsync()).ShouldNotBeNull(), TestContext.Current.CancellationToken)).ShouldContain("Current Jordan");
        await page.GetByLabel("Saved results", new() { Exact = true }).SelectOptionAsync(editionId.ToString());
        var recordedLink = page.GetByRole(AriaRole.Link, new() { Name = "Download saved results (CSV)", Exact = true });
        (await recordedLink.GetAttributeAsync("href")).ShouldEndWith($"?editionId={editionId}");
        await recordedLink.FocusAsync();
        await page.GetByLabel("Find a result", new() { Exact = true }).FillAsync("");
        await page.GetByText("3 of 3 results shown", new() { Exact = true }).WaitForAsync();
        await CapturePreparationAsync(session, "recorded-edition-export");
        var download = await page.RunAndWaitForDownloadAsync(() => recordedLink.ClickAsync());
        download.SuggestedFilename.ShouldBe($"Pino-recorded-results-{editionId}.csv");
        (await File.ReadAllTextAsync((await download.PathAsync()).ShouldNotBeNull(), TestContext.Current.CancellationToken)).ShouldNotContain("Current Jordan");
    }

    private static async Task VerifyAttendancePrintAsync(BrowserSession session, CloseoutFixture data)
    {
        var page = session.Page;
        await page.GotoAsync($"/clubs/{data.ClubId}/tryouts/{data.Tryout.Id}/print");
        await page.GetByRole(AriaRole.Button, new() { Name = "Print list", Exact = true }).WaitForAsync();
        await page.Locator("#print-order:enabled").WaitForAsync();
        (await page.Locator("tbody tr").CountAsync()).ShouldBe(2);
        (await page.Locator(".attendance-sheet").InnerTextAsync()).ShouldContain("Absent");
        (await page.Locator(".attendance-sheet").InnerTextAsync()).ShouldNotContain("Present");
        (await page.Locator(".attendance-sheet").InnerTextAsync()).ShouldNotContain("private-player@example.test");
        await page.GetByLabel("List order", new() { Exact = true }).SelectOptionAsync("bib");
        (await page.Locator("tbody tr").First.InnerTextAsync()).ShouldContain("Current Jordan");
        await page.GetByRole(AriaRole.Button, new() { Name = "Print list", Exact = true }).FocusAsync();
        await CapturePreparationAsync(session, "attendance-print-list");
        // Observe the browser print event without opening an unattended OS print dialog.
        await page.EvaluateAsync("() => { window.pinoPrintInvoked = false; window.print = () => { window.pinoPrintInvoked = true; }; }");
        await page.GetByRole(AriaRole.Button, new() { Name = "Print list", Exact = true }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.WaitForFunctionAsync("window.pinoPrintInvoked === true");
        await page.EmulateMediaAsync(new() { Media = Media.Print });
        (await page.Locator(".print-controls").IsVisibleAsync()).ShouldBeFalse();
        (await page.Locator(".app-masthead").IsVisibleAsync()).ShouldBeFalse();
        (await page.Locator(".club-context").IsVisibleAsync()).ShouldBeFalse();
        (await page.Locator("thead").EvaluateAsync<string>("element => getComputedStyle(element).display")).ShouldBe("table-header-group");
        await session.CaptureAsync("attendance-print-media");
        if (Environment.GetEnvironmentVariable("PINO_BROWSER_ARTIFACTS") is { Length: > 0 } output)
        {
            await page.PdfAsync(new() { Path = Path.Combine(output, "attendance-list.pdf"), Format = "A4", Margin = new() { Top = "12mm", Bottom = "12mm", Left = "12mm", Right = "12mm" } });
        }
        await page.EmulateMediaAsync(new() { Media = Media.Screen });
    }
}
