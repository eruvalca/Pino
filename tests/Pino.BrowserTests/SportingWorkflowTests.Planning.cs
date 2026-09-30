using Microsoft.Playwright;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task RosterPlanningWarnsWithoutBlockingPlacementsAndFiltersPreserveDraftsAsync()
    {
        await using var session = await BrowserSession.CreateAsync();
        await session.RegisterAsync();
        await session.CompleteProfileAsync(throughUi: false);
        var data = await PrepareCloseoutAsync(session);
        var path = $"/api/clubs/{data.ClubId}/sport";
        var tryoutPath = $"{path}/tryouts/{data.Tryout.Id}";
        var roster = await session.GetAsync<TryoutDetail>(tryoutPath);
        foreach (var entry in roster.Roster)
        {
            var player = entry.Player;
            var input = new PlayerInput
            {
                Id = player.Id,
                Revision = player.Revision,
                PlayerReference = player.PlayerReference,
                FirstName = player.FirstName,
                LastName = player.LastName,
                GraduationYear = player.GraduationYear,
                Position = player.Id == data.Player.Id ? "Midfield" : "Keeper",
                SecondaryPosition = player.Id == data.Player.Id ? "Keeper" : "Midfield",
            };
            (await session.PostAsync<SportReply>(path + "/players", input)).Kind.ShouldBe(SportReplyKind.Saved);
        }
        await VerifyTargetValidationAsync(session, data);
        var page = session.Page;
        await page.GotoAsync($"/clubs/{data.ClubId}/teams/{data.Blue.Id}/targets");
        await page.Locator("#target-total:enabled").WaitForAsync();
        await page.GetByLabel("Total roster target (optional)", new() { Exact = true }).FillAsync("1");
        await page.GetByRole(AriaRole.Button, new() { Name = "Add position target", Exact = true }).ClickAsync();
        await page.GetByLabel("Position", new() { Exact = true }).FillAsync("Keeper");
        await page.GetByLabel("Players", new() { Exact = true }).FillAsync("2");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save targets", Exact = true }).FocusAsync();
        await page.GetByText("Position targets add up to more than the total roster target. You can still save these targets.", new() { Exact = true }).WaitForAsync();
        await CapturePreparationAsync(session, "roster-target-editing");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save targets", Exact = true }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.GetByText("Roster targets saved. Targets never stop a valid placement.", new() { Exact = true }).WaitForAsync();
        var saved = await session.GetAsync<TeamDetail>($"{path}/teams/{data.Blue.Id}");
        saved.Team.RosterTarget.ShouldBe(1);
        saved.Team.PositionTargets.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBe(new("Keeper", 2));
        var stale = new TeamTargetsInput(saved.Team.Revision - 1, 9, []);
        (await session.PostAsync<SportReply>($"{path}/teams/{data.Blue.Id}/targets", stale)).Kind.ShouldBe(SportReplyKind.Conflict);
        var avery = roster.Roster.Single(value => string.Equals(value.Player.FirstName, "Avery", StringComparison.Ordinal));
        await PlaceAsync(session, path, data.Tryout.Id, avery.Player.Id, data.Blue.Id);
        await page.GotoAsync($"/clubs/{data.ClubId}/teams/{data.Blue.Id}");
        await page.Locator(".sport-heading button:enabled").WaitForAsync();
        await page.GetByText("2 players", new() { Exact = true }).WaitForAsync();
        (await page.Locator(".team-coverage").InnerTextAsync()).ShouldContain("Target 1: 1 over");
        (await page.Locator(".coverage-list").InnerTextAsync()).ShouldContain("Target 2: 1 short");
        await CapturePreparationAsync(session, "team-coverage");
        await page.GotoAsync($"/clubs/{data.ClubId}/seasons/{data.Season.Id}/review");
        await page.Locator(".sport-heading button:enabled").WaitForAsync();
        await page.GetByRole(AriaRole.Heading, new() { Name = data.Season.Name, Exact = true }).WaitForAsync();
        await CapturePreparationAsync(session, "season-coverage");
        await VerifyFocusedComparisonAsync(session, data);
        await VerifyPlanningFiltersAsync(session, data);
        await VerifyTargetClearAndArchiveAsync(session, data);
    }

    private static async Task VerifyFocusedComparisonAsync(BrowserSession session, CloseoutFixture data)
    {
        var page = session.Page;
        await page.GetByRole(AriaRole.Link, new() { Name = "Compare teams & notes", Exact = true }).ClickAsync();
        await page.Locator("#comparison-first:enabled").WaitForAsync();
        await page.GetByLabel("First team", new() { Exact = true }).SelectOptionAsync(data.Blue.Id.ToString());
        await page.GetByLabel("Second team", new() { Exact = true }).SelectOptionAsync(data.Silver.Id.ToString());
        await page.Locator(".comparison-player").Filter(new() { HasText = "Jordan Rivera" }).ClickAsync();
        await page.GetByText(data.Note.Text, new() { Exact = true }).WaitForAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = $"Preview on {data.Silver.Name}", Exact = true }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.GetByText("Preview only. No placement has been saved.", new() { Exact = true }).WaitForAsync();
        var saved = await session.GetAsync<TeamDetail>($"/api/clubs/{data.ClubId}/sport/teams/{data.Blue.Id}");
        saved.Members.ShouldContain(value => value.Id == data.Player.Id);
        await CapturePreparationAsync(session, "focused-team-comparison");
        await page.SetViewportSizeAsync(390, 844);
        await page.GetByRole(AriaRole.Button, new() { Name = "Back to rosters", Exact = true }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.Locator("#comparison-rosters-title:focus").WaitForAsync();
        (await page.Locator(":focus").GetAttributeAsync("id")).ShouldBe("comparison-rosters-title");
        (await page.Locator(".comparison-notebook").IsVisibleAsync()).ShouldBeFalse();
        (await page.Locator(".comparison-player[aria-pressed='true']").InnerTextAsync()).ShouldContain("Jordan Rivera");
        await page.GetByLabel("Player name or graduation year", new() { Exact = true }).FillAsync("Jordan");
        await session.CaptureAsync("focused-team-comparison-rosters-mobile");
        await page.GetByRole(AriaRole.Button, new() { Name = "Return to Jordan Rivera", Exact = true }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.Locator("#comparison-player-title:focus").WaitForAsync();
        (await page.Locator(":focus").GetAttributeAsync("id")).ShouldBe("comparison-player-title");
        await page.GetByRole(AriaRole.Button, new() { Name = "Review preview coverage", Exact = true }).ClickAsync();
        await page.Locator("#comparison-rosters-title:focus").WaitForAsync();
        (await page.Locator(":focus").GetAttributeAsync("id")).ShouldBe("comparison-rosters-title");
        (await page.GetByLabel("Player name or graduation year", new() { Exact = true }).InputValueAsync()).ShouldBe("Jordan");
        (await page.Locator(".preview-label").CountAsync()).ShouldBe(2);
        await page.GetByRole(AriaRole.Button, new() { Name = "Return to Jordan Rivera", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Clear preview", Exact = true }).ClickAsync();
        await page.Locator(".preview-label").First.WaitForAsync(new() { State = WaitForSelectorState.Detached });
        (await page.Locator(".preview-label").CountAsync()).ShouldBe(0);
        await page.SetViewportSizeAsync(1440, 1000);
        await page.GetByRole(AriaRole.Link, new() { Name = "Open tryout to save a decision", Exact = true }).ClickAsync();
        await page.GetByLabel("Add shared note", new() { Exact = true }).WaitForAsync();
        (await page.Locator(".notebook-heading h2").InnerTextAsync()).ShouldContain("Jordan");
    }

    private static async Task VerifyTargetValidationAsync(BrowserSession session, CloseoutFixture data)
    {
        var path = $"/api/clubs/{data.ClubId}/sport/teams/{data.Blue.Id}/targets";
        foreach (var input in new TeamTargetsInput[]
        {
            new(1, -1, []), new(1, 2001, []), new(1, null, [new("", 1)]), new(1, null, [new(new('X', 81), 1)]),
            new(1, null, [new("Keeper", -1)]), new(1, null, [new("Keeper", 2001)]), new(1, null, [new("Keeper", 1), new(" keeper ", 2)]),
            new(1, null, Enumerable.Range(0, 101).Select(value => new PositionTarget($"Position {value}", 1)).ToArray()), new(1, null, null!),
        })
        {
            (await session.PostAsync<SportReply>(path, input)).Kind.ShouldBe(SportReplyKind.Invalid);
        }
        var maximum = new TeamTargetsInput(1, 2000, Enumerable.Range(0, 100).Select(value => new PositionTarget($"Position {value}", 2000)).ToArray());
        (await session.PostAsync<SportReply>(path, maximum)).Kind.ShouldBe(SportReplyKind.Saved);
        var read = await session.GetAsync<TeamDetail>($"/api/clubs/{data.ClubId}/sport/teams/{data.Blue.Id}");
        read.Team.PositionTargets.ShouldNotBeNull().Count.ShouldBe(100);
        (await session.PostAsync<SportReply>(path, new TeamTargetsInput(read.Team.Revision, null, []))).Kind.ShouldBe(SportReplyKind.Saved);
    }

    private static async Task VerifyPlanningFiltersAsync(BrowserSession session, CloseoutFixture data)
    {
        var path = $"/api/clubs/{data.ClubId}/sport";
        var previous = new SeasonInput { Name = "Autumn 2026", StartsOn = new(2026, 7, 1), EndsOn = new(2026, 12, 31) };
        var team = new TeamInput { Name = "Previous Blue", GraduationYear = 2030 };
        var tryout = new TryoutInput { SeasonId = previous.Id, Name = "Previous evaluations", Date = new(2026, 8, 1) };
        (await session.PostAsync<SportReply>(path + "/seasons", previous)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(path + "/teams", team)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(path + "/tryouts", tryout)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.GetAsync<TryoutDetail>($"{path}/tryouts/{tryout.Id}")).Roster.ShouldContain(value => value.Player.Id == data.Player.Id);
        await PlaceAsync(session, path, tryout.Id, data.Player.Id, team.Id);
        var page = session.Page;
        await page.GotoAsync($"/clubs/{data.ClubId}/tryouts/{data.Tryout.Id}");
        await page.Locator("#player-note:enabled").WaitForAsync();
        await page.Locator(".roster-record").Filter(new() { HasText = "Jordan Rivera" }).ClickAsync();
        await page.GetByLabel("Add shared note", new() { Exact = true }).FillAsync("Keep this unfinished observation while filtering.");
        await page.Locator(".planning-filters summary").FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.GetByLabel("Primary or secondary position", new() { Exact = true }).SelectOptionAsync("Keeper");
        (await page.Locator(".roster-record").CountAsync()).ShouldBe(3);
        await page.GetByLabel("Notes in this tryout", new() { Exact = true }).SelectOptionAsync("missing");
        (await page.Locator(".roster-record").CountAsync()).ShouldBe(2);
        await page.GetByLabel("Compare with season", new() { Exact = true }).SelectOptionAsync(previous.Id.ToString());
        await page.GetByLabel("Team in comparison season", new() { Exact = true }).SelectOptionAsync(team.Id.ToString());
        await page.GetByText("No matches. Change your filters to see more players.", new() { Exact = true }).WaitForAsync();
        await page.GetByLabel("Notes in this tryout", new() { Exact = true }).SelectOptionAsync("");
        (await page.Locator(".roster-record").CountAsync()).ShouldBe(1);
        await session.CaptureAsync("planning-filters-desktop");
        await page.SetViewportSizeAsync(390, 844);
        await page.GetByRole(AriaRole.Button, new() { Name = "Back to roster", Exact = true }).ClickAsync();
        await session.CaptureAsync("planning-filters-mobile");
        await page.SetViewportSizeAsync(1440, 1000);
        (await page.GetByLabel("Add shared note", new() { Exact = true }).InputValueAsync()).ShouldBe("Keep this unfinished observation while filtering.");
        await page.GetByRole(AriaRole.Button, new() { Name = "Clear player filters", Exact = true }).ClickAsync();
        (await page.Locator(".roster-record").CountAsync()).ShouldBe(3);
        (await session.PostAsync<SportReply>($"{path}/tryouts/{data.Tryout.Id}/notes/redact", new RedactNoteInput(Guid.NewGuid(), data.Note.Id, "Synthetic redaction to verify missing-observation filter", true))).Kind.ShouldBe(SportReplyKind.Saved);
        await page.Locator(".supporting-tools > summary").ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Refresh saved decisions", Exact = true }).ClickAsync();
        await page.GetByText("Saved decisions refreshed. Unsaved notes are still here.", new() { Exact = true }).WaitForAsync();
        await page.GetByLabel("Notes in this tryout", new() { Exact = true }).SelectOptionAsync("missing");
        (await page.Locator(".roster-record").CountAsync()).ShouldBe(3);
        (await page.GetByLabel("Add shared note", new() { Exact = true }).InputValueAsync()).ShouldBe("Keep this unfinished observation while filtering.");
    }

    private static async Task VerifyTargetClearAndArchiveAsync(BrowserSession session, CloseoutFixture data)
    {
        var path = $"/api/clubs/{data.ClubId}/sport";
        var team = (await session.GetAsync<TeamDetail>($"{path}/teams/{data.Blue.Id}")).Team;
        (await session.PostAsync<SportReply>($"{path}/teams/{team.Id}/targets", new TeamTargetsInput(team.Revision, 0, [new(" keeper ", 0)]))).Kind.ShouldBe(SportReplyKind.Saved);
        team = (await session.GetAsync<TeamDetail>($"{path}/teams/{team.Id}")).Team;
        team.PositionTargets.ShouldNotBeNull().ShouldHaveSingleItem().ShouldBe(new("keeper", 0));
        (await session.PostAsync<SportReply>($"{path}/teams/{team.Id}/targets", new TeamTargetsInput(team.Revision, null, []))).Kind.ShouldBe(SportReplyKind.Saved);
        team = (await session.GetAsync<TeamDetail>($"{path}/teams/{team.Id}")).Team;
        team.RosterTarget.ShouldBeNull();
        team.PositionTargets.ShouldNotBeNull().ShouldBeEmpty();
        data.Silver.Revision = 1; data.Silver.Archived = true;
        (await session.PostAsync<SportReply>(path + "/teams", data.Silver)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>($"{path}/teams/{data.Silver.Id}/targets", new TeamTargetsInput(2, 1, []))).Kind.ShouldBe(SportReplyKind.Invalid);
        data.Season.Revision = 1; data.Season.Archived = true;
        (await session.PostAsync<SportReply>(path + "/seasons", data.Season)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>($"{path}/teams/{team.Id}/targets", new TeamTargetsInput(team.Revision, 1, []))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.GetAsync<TeamDetail>($"{path}/teams/{team.Id}")).Team.RosterTarget.ShouldBe(1);
    }
}
