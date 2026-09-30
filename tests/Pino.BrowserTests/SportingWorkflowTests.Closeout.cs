using Microsoft.Playwright;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task ReviewedCloseoutsLockEditsPreserveEditionsAndKeepCurrentRostersIndependentAsync()
    {
        await using var session = await BrowserSession.CreateAsync();
        await session.RegisterAsync();
        await session.CompleteProfileAsync(throughUi: false);
        var data = await PrepareCloseoutAsync(session);
        var path = $"/api/clubs/{data.ClubId}/sport";
        var tryoutPath = $"{path}/tryouts/{data.Tryout.Id}";
        var stale = await session.GetAsync<TryoutReview>(tryoutPath + "/review");
        (await session.PostAsync<SportReply>(tryoutPath + "/bib", new BibNumberInput(data.Player.Id, "41", "17"))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/close", new CloseTryoutInput(Guid.NewGuid(), stale.ReviewToken))).Kind.ShouldBe(SportReplyKind.Conflict);
        var before = await session.GetAsync<SeasonReview>($"{path}/seasons/{data.Season.Id}/review");
        before.Players.ShouldBe(3);
        before.Teams.Single(value => value.Team.Id == data.Blue.Id).Players.ShouldHaveSingleItem().Id.ShouldBe(data.Player.Id);
        before.UnplacedPlayers.Count.ShouldBe(2);

        var notebook = await session.Context.NewPageAsync();
        await notebook.SetViewportSizeAsync(390, 844);
        await notebook.GotoAsync($"/clubs/{data.ClubId}/tryouts/{data.Tryout.Id}");
        await notebook.Locator(".tryout-heading .primary-action:not([disabled])").WaitForAsync();
        await notebook.GetByRole(AriaRole.Button, new() { Name = "Add players", Exact = true }).FocusAsync();
        await notebook.Keyboard.PressAsync("Enter");
        await notebook.GetByRole(AriaRole.Heading, new() { Name = "Add players from your catalog", Exact = true }).WaitForAsync();
        await notebook.Locator(".roster-record").Filter(new() { HasText = "Jordan Rivera" }).ClickAsync();
        await notebook.GetByLabel("Add shared note", new() { Exact = true }).FillAsync("Keep this draft when another coach closes the tryout.");

        var page = session.Page;
        await page.GotoAsync($"/clubs/{data.ClubId}/tryouts/{data.Tryout.Id}/review");
        await page.Locator(".closeout-action fieldset:not([disabled])").WaitForAsync();
        (await page.GetByRole(AriaRole.Button, new() { Name = "Close tryout & record results", Exact = true }).IsEnabledAsync()).ShouldBeFalse();
        await page.GetByLabel("I have reviewed all 3 player outcomes.").CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Close tryout & record results", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Heading, new() { Name = "Results recorded", Exact = true }).WaitForAsync();
        await notebook.GetByRole(AriaRole.Button, new() { Name = "Refresh saved decisions", Exact = true }).FocusAsync();
        await notebook.Keyboard.PressAsync("Enter");
        await notebook.Locator("#enrollment-heading").WaitForAsync(new() { State = WaitForSelectorState.Detached });
        (await notebook.GetByRole(AriaRole.Button, new() { Name = "Add players", Exact = true }).IsDisabledAsync()).ShouldBeTrue();
        (await notebook.GetByLabel("Add shared note", new() { Exact = true }).InputValueAsync()).ShouldBe("Keep this draft when another coach closes the tryout.");
        (await notebook.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth")).ShouldBeTrue();
        await notebook.CloseAsync();
        var closed = await session.GetAsync<TryoutReview>(tryoutPath + "/review");
        closed.Tryout.Closed.ShouldBeTrue();
        var edition = closed.Closeouts.ShouldHaveSingleItem();
        edition.ClosedBy.ShouldBe("Avery Coach");
        edition.Results.Select(value => value.Decision).ShouldBe([DecisionKind.Withdrawn, DecisionKind.NotSelected, DecisionKind.Placed], ignoreOrder: true);
        edition.Results.Single(value => value.PlayerId == data.Player.Id).Bib.ShouldBe("41");
        (await session.GetAsync<SeasonReview>($"{path}/seasons/{data.Season.Id}/review")).Teams.Single(value => value.Team.Id == data.Blue.Id).Players.ShouldHaveSingleItem().Id.ShouldBe(data.Player.Id);
        await CaptureReviewAsync(session, "closeout");
        await VerifyCloseoutLocksAsync(session, data, edition.Id);
        await VerifyCloseoutHistoryAsync(session, data, edition);
        await VerifyCloseoutReopeningAsync(session, data, edition);
    }

    private static async Task<CloseoutFixture> PrepareCloseoutAsync(BrowserSession session)
    {
        (await session.PostAsync<ClubReply>("/api/clubs/create", new CreateClubInput { Name = "Test · Season notebook", Sport = "Soccer", City = "Chicago", State = "IL" })).Kind.ShouldBe(ClubReplyKind.Saved);
        var clubId = (await session.GetAsync<AccessSnapshot>("/api/clubs/access")).Membership.ShouldNotBeNull().Club.Id;
        var path = $"/api/clubs/{clubId}/sport";
        var season = new SeasonInput { Name = "Spring 2027", StartsOn = new(2027, 1, 1), EndsOn = new(2027, 6, 30) };
        var blue = new TeamInput { SeasonId = season.Id, Name = "Northside Blue", GraduationYear = 2030 };
        var silver = new TeamInput { SeasonId = season.Id, Name = "Northside Silver", GraduationYear = 2030 };
        var tryout = new TryoutInput { SeasonId = season.Id, Name = "Spring field evaluations", Date = new(2027, 2, 20), Location = "Lincoln Park · Field 3" };
        var player = new PlayerInput { FirstName = "Jordan", LastName = "Rivera", GraduationYear = 2030 };
        (await session.PostAsync<SportReply>(path + "/seasons", season)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(path + "/teams", blue)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(path + "/teams", silver)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(path + "/tryouts", tryout)).Kind.ShouldBe(SportReplyKind.Saved);
        var tryoutPath = $"{path}/tryouts/{tryout.Id}";
        var empty = await session.GetAsync<TryoutReview>(tryoutPath + "/review");
        (await session.PostAsync<SportReply>(tryoutPath + "/close", new CloseTryoutInput(Guid.NewGuid(), empty.ReviewToken))).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(path + "/players", player)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/players", new EnrollmentInput(player.Id, "17"))).Kind.ShouldBe(SportReplyKind.Saved);
        var incomplete = await session.GetAsync<TryoutReview>(tryoutPath + "/review");
        (await session.PostAsync<SportReply>(tryoutPath + "/close", new CloseTryoutInput(Guid.NewGuid(), incomplete.ReviewToken))).Kind.ShouldBe(SportReplyKind.Invalid);
        await PlaceAsync(session, path, tryout.Id, player.Id, blue.Id);
        foreach (var (first, last, bib, kind) in new[] { ("Avery", "Chen", "22", DecisionKind.Withdrawn), ("Morgan", "Patel", "35", DecisionKind.NotSelected) })
        {
            var other = new PlayerInput { FirstName = first, LastName = last, GraduationYear = 2031 };
            (await session.PostAsync<SportReply>(path + "/players", other)).Kind.ShouldBe(SportReplyKind.Saved);
            (await session.PostAsync<SportReply>(tryoutPath + "/players", new EnrollmentInput(other.Id, bib))).Kind.ShouldBe(SportReplyKind.Saved);
            (await session.PostAsync<SportReply>(tryoutPath + "/decisions", new DecisionInput(Guid.NewGuid(), other.Id, kind, null, 1, 0, "Recorded during review."))).Kind.ShouldBe(SportReplyKind.Saved);
        }
        var note = new NoteInput(Guid.NewGuid(), player.Id, "Scans before receiving and finds the far-side runner.");
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", note)).Kind.ShouldBe(SportReplyKind.Saved);
        return new(clubId, season, blue, silver, tryout, player, note);
    }

    private sealed record CloseoutFixture(Guid ClubId, SeasonInput Season, TeamInput Blue, TeamInput Silver, TryoutInput Tryout, PlayerInput Player, NoteInput Note);
}
