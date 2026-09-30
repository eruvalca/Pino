using Microsoft.Playwright;
using Npgsql;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task MaximumTryoutCanBeReviewedFilteredAndClosedAsync()
    {
        await using var session = await BrowserSession.CreateAsync();
        await session.RegisterAsync();
        await session.CompleteProfileAsync(throughUi: false);
        (await session.PostAsync<ClubReply>("/api/clubs/create", new CreateClubInput { Name = "Test · Maximum roster", Sport = "Soccer", City = "Chicago", State = "IL" })).Kind.ShouldBe(ClubReplyKind.Saved);
        var clubId = (await session.GetAsync<AccessSnapshot>("/api/clubs/access")).Membership.ShouldNotBeNull().Club.Id;
        var path = $"/api/clubs/{clubId}/sport";
        var season = new SeasonInput { Name = "Maximum roster season", StartsOn = new(2027, 1, 1), EndsOn = new(2027, 6, 30) };
        var tryout = new TryoutInput { Name = "Maximum roster review", SeasonId = season.Id, Date = new(2027, 2, 1) };
        (await session.PostAsync<SportReply>(path + "/seasons", season)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(path + "/tryouts", tryout)).Kind.ShouldBe(SportReplyKind.Saved);
        await SeedMaximumRosterAsync(clubId, season.Id, tryout.Id);
        var review = await session.GetAsync<TryoutReview>($"{path}/tryouts/{tryout.Id}/review");
        review.Results.Count.ShouldBe(2000);
        review.Tryout.Complete.ShouldBeTrue();
        await session.Page.GotoAsync($"/clubs/{clubId}/tryouts/{tryout.Id}/review");
        await session.Page.Locator("#result-search:enabled").WaitForAsync();
        await session.Page.GetByLabel("Find a result", new() { Exact = true }).FillAsync("00000000000000002000");
        await session.Page.GetByText("1 of 2000 results match", new() { Exact = true }).WaitForAsync();
        (await session.Page.Locator(".result-ledger li").CountAsync()).ShouldBe(1);
        await session.Page.SetViewportSizeAsync(320, 800);
        (await session.Page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth")).ShouldBeTrue("Maximum name and bib lengths must fit a narrow review.");
        await session.CaptureAsync("closeout-long-content-narrow");
        await session.Page.GetByLabel("Result", new() { Exact = true }).SelectOptionAsync("Placed");
        await session.Page.GetByText("No results match these filters.", new() { Exact = true }).WaitForAsync();
        (await session.Page.Locator(".result-ledger li").CountAsync()).ShouldBe(0);
        var result = await session.PostAsync<SportReply>($"{path}/tryouts/{tryout.Id}/close", new CloseTryoutInput(Guid.NewGuid(), review.ReviewToken));
        result.Kind.ShouldBe(SportReplyKind.Saved, result.Message);
        var saved = await session.GetAsync<TryoutReview>($"{path}/tryouts/{tryout.Id}/review");
        saved.Closeouts.ShouldHaveSingleItem().Results.Count.ShouldBe(2000);
        saved.Closeouts[0].Results.ShouldAllBe(value => value.FirstName.Length == 80 && value.MiddleName.Length == 80 && value.LastName.Length == 80 && value.Bib.Length == 20 && value.Decision == DecisionKind.DidNotAttend);
    }

    private static async Task SeedMaximumRosterAsync(Guid clubId, Guid seasonId, Guid tryoutId)
    {
        // Seed only this test's disposable club to exercise the maximum read/close
        // path without spending 2,000 HTTP operations on the separate enrollment flow.
        const string Sql = """
            INSERT INTO "Players" ("Id", "ClubId", "PlayerReference", "FirstName", "MiddleName", "LastName", "GraduationYear", "Position", "SecondaryPosition", "ContactEmail", "Archived", "Revision")
            SELECT id, @club, id::text, repeat('A', 80), repeat('M', 80), repeat('Z', 80), 2030, '', '', '', false, 1 FROM unnest(@ids) AS id;
            INSERT INTO "Participations" ("ClubId", "TryoutId", "PlayerId", "Bib", "Decision", "Revision", "Removed")
            SELECT @club, @tryout, id, lpad(number::text, 20, '0'), @decision, 1, false FROM unnest(@ids) WITH ORDINALITY AS entries(id, number);
            INSERT INTO "SeasonPlacements" ("ClubId", "SeasonId", "PlayerId", "Revision")
            SELECT @club, @season, id, 0 FROM unnest(@ids) AS id;
            """;
        await using var connection = new NpgsqlConnection(Environment.GetEnvironmentVariable("PINO_TEST_DATABASE"));
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        await using var command = new NpgsqlCommand(Sql, connection);
        command.Parameters.AddWithValue("club", clubId);
        command.Parameters.AddWithValue("season", seasonId);
        command.Parameters.AddWithValue("tryout", tryoutId);
        command.Parameters.AddWithValue("decision", (int)DecisionKind.DidNotAttend);
        command.Parameters.AddWithValue("ids", Enumerable.Range(0, 2000).Select(_ => Guid.NewGuid()).ToArray());
        await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
    }
}
