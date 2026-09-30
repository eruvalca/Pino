using System.Globalization;
using CsvHelper;
using Microsoft.Playwright;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    [Fact(Skip = "Requires local Aspire and an explicitly supplied PINO_PLAYER_CSV_PATH registration export.", SkipUnless = nameof(BrowserEnvironment.SuppliedCsvEnabled), SkipType = typeof(BrowserEnvironment))]
    public async Task SuppliedRegistrationCsvMapsGraduationYearsAndPopulatesTryoutAsync()
    {
        // The private source stays outside the repository. This fixture creates no screenshots or retained payloads.
        var file = Environment.GetEnvironmentVariable("PINO_PLAYER_CSV_PATH")!;
        using var reader = new StreamReader(file);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        (await csv.ReadAsync()).ShouldBeTrue();
        csv.ReadHeader();
        var expected = new HashSet<(string FirstName, string LastName, int GraduationYear)>();
        while (await csv.ReadAsync()) { expected.Add((csv.GetField("player_first_name")!.Trim(), csv.GetField("player_last_name")!.Trim(), csv.GetField<int>("grad_year"))); }
        expected.Count.ShouldBeInRange(1, 1000);
        await using var session = await BrowserSession.CreateAsync();
        session.CaptureDiagnostics = false;
        await session.RegisterAsync();
        await session.CompleteProfileAsync(throughUi: false);
        (await session.PostAsync<ClubReply>("/api/clubs/create", new CreateClubInput { Name = "Test · Supplied registration export", Sport = "Soccer", City = "Chicago", State = "IL" })).Kind.ShouldBe(ClubReplyKind.Saved);
        var clubId = (await session.GetAsync<AccessSnapshot>("/api/clubs/access")).Membership.ShouldNotBeNull().Club.Id;
        var path = $"/api/clubs/{clubId}/sport";
        await session.Page.GotoAsync($"/clubs/{clubId}/players/import");
        await session.Page.Locator("#import-file:enabled").WaitForAsync();
        await session.Page.GetByLabel("Player CSV", new() { Exact = true }).SetInputFilesAsync(file);
        await session.Page.GetByLabel("Graduation year", new() { Exact = true }).SelectOptionAsync(Array.IndexOf(csv.HeaderRecord!, "grad_year").ToString(CultureInfo.InvariantCulture));
        // This export reformatted many identifiers. Generate Pino references and omit family account contact data.
        await session.Page.GetByLabel("Player ID from file (optional)", new() { Exact = true }).SelectOptionAsync("-1");
        await session.Page.GetByLabel("Contact email (optional)", new() { Exact = true }).SelectOptionAsync("-1");
        await session.Page.GetByRole(AriaRole.Button, new() { Name = "Preview mapped file", Exact = true }).ClickAsync();
        await session.Page.GetByRole(AriaRole.Button, new() { Name = "Import reviewed players", Exact = true }).ClickAsync();
        await session.Page.GetByRole(AriaRole.Link, new() { Name = "Open players", Exact = true }).ClickAsync();
        await session.Page.GetByRole(AriaRole.Heading, new() { Name = "Players", Exact = true }).WaitForAsync();
        var players = new List<PlayerSummary>();
        for (var page = 0; page < expected.Count; page++)
        {
            var current = await session.GetAsync<PlayerPage>($"{path}/players?query=&archived=false&page={page}");
            players.AddRange(current.Players);
            if (!current.HasMore) { break; }
        }
        players.Count.ShouldBe(expected.Count);
        expected.SetEquals(players.Select(value => (value.FirstName, value.LastName, value.GraduationYear))).ShouldBeTrue("Every source player's name and graduation year must match the imported catalog.");
        players.ShouldAllBe(value => value.ContactEmail.Length == 0, "Unmapped account and family data must not enter player contact fields.");
        var season = new SeasonInput { Name = "Registration export evaluation", StartsOn = new(2027, 1, 1), EndsOn = new(2027, 6, 30) };
        var tryout = new TryoutInput { SeasonId = season.Id, Name = "Whole catalog", Date = new(2027, 2, 1) };
        (await session.PostAsync<SportReply>(path + "/seasons", season)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(path + "/tryouts", tryout)).Kind.ShouldBe(SportReplyKind.Saved);
        var roster = await session.GetAsync<TryoutDetail>($"{path}/tryouts/{tryout.Id}");
        roster.Roster.Count.ShouldBe(expected.Count);
        roster.Roster.Select(value => value.Player.Id).ToHashSet().SetEquals(players.Select(value => value.Id)).ShouldBeTrue();
        await session.Page.GotoAsync($"/clubs/{clubId}/tryouts/{tryout.Id}");
        await session.Page.Locator("#roster-query:enabled").WaitForAsync();
        (await session.Page.Locator(".roster-record").CountAsync()).ShouldBe(Math.Min(50, expected.Count));
    }
}
