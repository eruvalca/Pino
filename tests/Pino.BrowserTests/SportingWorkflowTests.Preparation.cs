using Microsoft.Playwright;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task ClubTeamsAndDefaultRostersPreservePriorSeasonsAndReviewedChangesAsync()
    {
        await using var session = await BrowserSession.CreateAsync();
        await session.RegisterAsync();
        await session.CompleteProfileAsync(throughUi: false);
        var data = await PrepareCloseoutAsync(session);
        var path = $"/api/clubs/{data.ClubId}/sport";
        var source = await session.GetAsync<TryoutDetail>($"{path}/tryouts/{data.Tryout.Id}");
        foreach (var player in source.Roster.Where(value => value.Player.Id != data.Player.Id)) { await PlaceAsync(session, path, data.Tryout.Id, player.Player.Id, data.Blue.Id); }
        var target = new SeasonInput { Name = "Autumn 2027", StartsOn = new(2027, 8, 1), EndsOn = new(2027, 12, 31) };
        (await session.PostAsync<SportReply>(path + "/seasons", target)).Kind.ShouldBe(SportReplyKind.Saved);
        var tryout = new TryoutInput { SeasonId = target.Id, Name = "Returning-player evaluations", Date = target.StartsOn.AddDays(10) };
        (await session.PostAsync<SportReply>(path + "/tryouts", tryout)).Kind.ShouldBe(SportReplyKind.Saved);
        var tryoutPath = $"{path}/tryouts/{tryout.Id}";
        var created = await session.GetAsync<TryoutDetail>(tryoutPath);
        created.Roster.Count.ShouldBe(3);
        created.Roster.ShouldAllBe(value => value.Decision == DecisionKind.Awaiting && value.Bib.Length == 0 && value.PreviousPlacement != null && value.PreviousPlacement.TeamId == data.Blue.Id);
        created.Teams.Select(value => value.Id).ShouldBe([data.Blue.Id, data.Silver.Id], ignoreOrder: true);
        await VerifyAvailabilityScopesAsync(session, data, target, tryout);
        await VerifySameTeamThroughUiAsync(session, data, tryout);
        var page = session.Page;
        var preview = await session.GetAsync<ReturningPlayerReview[]>(tryoutPath + $"/returning?sourceSeasonId={data.Season.Id}");
        preview.Count(value => value.Selection is not null).ShouldBe(2);
        await page.GotoAsync($"/clubs/{data.ClubId}/tryouts/{tryout.Id}/returning");
        await page.GetByRole(AriaRole.Button, new() { Name = "Continue to players", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Select ready matches", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Review selected placements", Exact = true }).ClickAsync();
        await CapturePreparationAsync(session, "returning-players");
        var changed = preview.Single(value => string.Equals(value.Player.FirstName, "Morgan", StringComparison.Ordinal)).Selection.ShouldNotBeNull();
        (await session.PostAsync<SportReply>(tryoutPath + "/decisions", new DecisionInput(Guid.NewGuid(), changed.PlayerId, DecisionKind.NotSelected, null, changed.EntryRevision, changed.PlacementRevision, "Staff decision during bulk review"))).Kind.ShouldBe(SportReplyKind.Saved);
        await page.GetByRole(AriaRole.Button, new() { Name = "Place reviewed players", Exact = true }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.GetByText("Batch completed: 1 applied, 1 skipped. Review the per-player results below.", new() { Exact = true }).WaitForAsync();
        var current = await session.GetAsync<TryoutDetail>(tryoutPath);
        current.Roster.Single(value => string.Equals(value.Player.FirstName, "Avery", StringComparison.Ordinal)).CurrentTeamId.ShouldBe(data.Blue.Id);
        current.Roster.Single(value => value.Player.Id == changed.PlayerId).Decision.ShouldBe(DecisionKind.NotSelected);
        (await session.GetAsync<PlayerNotebook>($"{tryoutPath}/players/{changed.PlayerId}/notebook")).History.ShouldHaveSingleItem().Reason.ShouldBe("Staff decision during bulk review");
        (await session.GetAsync<TeamDetail>($"{path}/teams/{data.Blue.Id}?seasonId={data.Season.Id}")).Members.Count.ShouldBe(3);
        (await session.GetAsync<TeamDetail>($"{path}/teams/{data.Blue.Id}?seasonId={target.Id}")).Members.Count.ShouldBe(2);
        await page.GotoAsync($"/clubs/{data.ClubId}/teams/{data.Blue.Id}?season={data.Season.Id}");
        await page.Locator("#roster-season:enabled").WaitForAsync();
        (await page.Locator(".record-list .record-row").CountAsync()).ShouldBe(3);
        await page.GetByLabel("Season roster", new() { Exact = true }).SelectOptionAsync(target.Id.ToString());
        await page.GetByRole(AriaRole.Link, new() { Name = "Download current team roster (CSV)", Exact = true }).WaitForAsync();
        await page.Locator($"a[download][href$='seasonId={target.Id}']").WaitForAsync();
        (await page.Locator(".record-list .record-row").CountAsync()).ShouldBe(2);
        (await page.GetByRole(AriaRole.Link, new() { Name = "Download current team roster (CSV)", Exact = true }).GetAttributeAsync("href")).ShouldEndWith($"?seasonId={target.Id}");
        await CapturePreparationAsync(session, "club-team-season-roster");
        var review = await session.GetAsync<TryoutReview>(tryoutPath + "/review");
        (await session.PostAsync<SportReply>(tryoutPath + "/close", new CloseTryoutInput(Guid.NewGuid(), review.ReviewToken))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>($"{path}/seasons/{target.Id}/team-availability?tryoutId={tryout.Id}", new TeamAvailabilityInput(data.Blue.Id, true, 3))).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportingBatchReport>(tryoutPath + "/returning", new ReturningPlacementInput(Guid.NewGuid(), data.Season.Id, [changed]))).Kind.ShouldBe(SportReplyKind.Invalid);
    }

    private static async Task VerifyAvailabilityScopesAsync(BrowserSession session, CloseoutFixture data, SeasonInput target, TryoutInput tryout)
    {
        var path = $"/api/clubs/{data.ClubId}/sport";
        var scope = $"{path}/seasons/{target.Id}/team-availability";
        var tryoutPath = $"{path}/tryouts/{tryout.Id}";
        (await session.PostAsync<SportReply>(scope, new TeamAvailabilityInput(Guid.NewGuid(), true, 0))).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(scope + $"?tryoutId={data.Tryout.Id}", new TeamAvailabilityInput(data.Blue.Id, true, 0))).Kind.ShouldBe(SportReplyKind.Invalid);
        var lateTeam = new TeamInput { Name = "New club team", GraduationYear = 2031 };
        (await session.PostAsync<SportReply>(path + "/teams", lateTeam)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.GetAsync<TryoutDetail>(tryoutPath)).Teams.ShouldContain(value => value.Id == lateTeam.Id && !value.Excluded);
        (await session.PostAsync<SportReply>(scope, new TeamAvailabilityInput(data.Blue.Id, true, 0))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(scope + $"?tryoutId={tryout.Id}", new TeamAvailabilityInput(data.Blue.Id, false, 0))).Kind.ShouldBe(SportReplyKind.Saved);
        var excluded = await session.GetAsync<TryoutDetail>(tryoutPath);
        excluded.Teams.Single(value => value.Id == data.Blue.Id).Excluded.ShouldBeTrue();
        var entry = excluded.Roster.Single(value => value.Player.Id == data.Player.Id);
        (await session.PostAsync<SportReply>(tryoutPath + "/decisions", new DecisionInput(Guid.NewGuid(), entry.Player.Id, DecisionKind.Placed, data.Blue.Id, entry.Revision, entry.PlacementRevision, "Excluded team"))).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.GetAsync<ReturningPlayerReview[]>(tryoutPath + $"/returning?sourceSeasonId={data.Season.Id}")).ShouldAllBe(value => value.Selection == null);
        (await session.GetAsync<TeamDetail>($"{path}/teams/{data.Blue.Id}?seasonId={data.Season.Id}")).Members.Count.ShouldBe(3);
        (await session.PostAsync<SportReply>(scope, new TeamAvailabilityInput(data.Blue.Id, false, 1))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(scope, new TeamAvailabilityInput(data.Blue.Id, true, 0))).Kind.ShouldBe(SportReplyKind.Conflict);
        (await session.PostAsync<SportReply>(scope + $"?tryoutId={tryout.Id}", new TeamAvailabilityInput(data.Blue.Id, true, 1))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.GetAsync<TryoutDetail>(tryoutPath)).Teams.Single(value => value.Id == data.Blue.Id).Excluded.ShouldBeTrue();
        (await session.GetAsync<SeasonReview>($"{path}/seasons/{target.Id}/review")).Teams.Single(value => value.Team.Id == data.Blue.Id).Team.Excluded.ShouldBeFalse();
        await session.Page.GotoAsync($"/clubs/{data.ClubId}/seasons/{target.Id}/teams?tryout={tryout.Id}");
        await session.Page.GetByRole(AriaRole.Button, new() { Name = $"Include team {data.Blue.Name}", Exact = true }).ClickAsync();
        await session.Page.GetByRole(AriaRole.Button, new() { Name = $"Exclude team {data.Blue.Name}", Exact = true }).WaitForAsync();
        await session.Page.Locator("button[aria-label='Exclude team Northside Blue']:enabled").WaitForAsync();
        await CapturePreparationAsync(session, "team-availability");
        (await session.GetAsync<TryoutDetail>(tryoutPath)).Teams.Single(value => value.Id == data.Blue.Id).Excluded.ShouldBeFalse();
    }

    private static async Task VerifySameTeamThroughUiAsync(BrowserSession session, CloseoutFixture data, TryoutInput tryout)
    {
        var page = session.Page;
        await page.GotoAsync($"/clubs/{data.ClubId}/tryouts/{tryout.Id}?player={data.Player.Id}");
        await page.Locator(".roster-record:enabled").First.WaitForAsync();
        await page.SetViewportSizeAsync(390, 844);
        (await page.Locator(".notebook-heading h2").EvaluateAsync<bool>("element => element.getBoundingClientRect().bottom + scrollY <= innerHeight")).ShouldBeTrue("The selected player's identity should be visible on phone entry.");
        await session.CaptureAsync("tryout-notebook-entry-mobile");
        await page.SetViewportSizeAsync(1440, 1000);
        await page.GetByLabel("Add shared note", new() { Exact = true }).FillAsync("Unsubmitted observation survives the quick placement");
        await page.GetByRole(AriaRole.Button, new() { Name = $"Place on {data.Blue.Name}", Exact = true }).ClickAsync();
        await page.GetByText("Same team recorded", new() { Exact = true }).WaitForAsync();
        await page.Locator(".player-notebook [role='status'][data-kind='success']").WaitForAsync();
        (await page.GetByLabel("Add shared note", new() { Exact = true }).InputValueAsync()).ShouldBe("Unsubmitted observation survives the quick placement");
        await CapturePreparationAsync(session, "previous-team-placement");
        await page.SetViewportSizeAsync(390, 844);
        (await page.Locator(".notebook-heading h2").EvaluateAsync<bool>("element => element.getBoundingClientRect().bottom + scrollY <= innerHeight")).ShouldBeTrue("The selected player's identity should be visible in the phone's first viewport.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Back to roster", Exact = true }).ClickAsync();
        await page.Locator(".evaluation-workspace[data-view='roster']").WaitForAsync();
        (await page.Locator("#roster-query").EvaluateAsync<bool>("element => element.getBoundingClientRect().bottom + scrollY <= innerHeight")).ShouldBeTrue("Player search should be visible in the phone's first viewport.");
        await session.CaptureAsync("tryout-roster-entry-mobile");
        await page.SetViewportSizeAsync(1440, 1000);
        var notebook = await session.GetAsync<PlayerNotebook>($"/api/clubs/{data.ClubId}/sport/tryouts/{tryout.Id}/players/{data.Player.Id}/notebook");
        notebook.History.ShouldHaveSingleItem().TeamId.ShouldBe(data.Blue.Id);
        notebook.Notes.ShouldBeEmpty();
    }

    private static async Task CapturePreparationAsync(BrowserSession session, string name)
    {
        await session.CaptureAsync(name + "-desktop");
        await session.Page.SetViewportSizeAsync(390, 844);
        await session.CaptureAsync(name + "-mobile");
        await session.Page.SetViewportSizeAsync(1440, 1000);
    }
}
