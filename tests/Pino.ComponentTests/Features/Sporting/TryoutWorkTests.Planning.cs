using Bunit;
using NSubstitute;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Components;
using Pino.UI.Features.Sporting.Pages;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Sporting;

public sealed partial class TryoutWorkTests
{
    [Fact]
    public async Task PositionCoverageSeparatesPrimaryFromAdditionalSecondaryAndShowsAdvisoryGapsAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var player = data.Roster[0].Player;
        var team = data.Teams[0] with { RosterTarget = 2, PositionTargets = [new("Midfield", 1), new("Keeper", 2)] };
        var coverage = context.Render<TeamCoverage>(parameters => parameters.Add(value => value.Team, team).Add(value => value.Players, new PlayerSummary[]
        {
            player with { Position = " midfield ", SecondaryPosition = "MIDFIELD" },
            player with { Id = Guid.NewGuid(), Position = "Keeper", SecondaryPosition = "Midfield" },
            player with { Id = Guid.NewGuid(), Position = "", SecondaryPosition = "Keeper" },
        }));
        coverage.Find(".notice").TextContent.ShouldContain("3 players · Target 2: 1 over");
        var rows = coverage.FindAll(".coverage-list li");
        rows.Count.ShouldBe(2);
        rows.Single(value => string.Equals(value.QuerySelector("strong")!.TextContent, "Midfield", StringComparison.Ordinal)).TextContent.ShouldContain("1 primary · 1 additional secondary");
        rows.Single(value => string.Equals(value.QuerySelector("strong")!.TextContent, "Keeper", StringComparison.Ordinal)).TextContent.ShouldContain("Target 2: 1 short");
        coverage.Markup.ShouldContain("1 player has no primary position recorded");
        coverage.Markup.ShouldContain("Position targets add up to more than the team total");
        coverage.Render(parameters => parameters.Add(value => value.Team, team with { RosterTarget = null, PositionTargets = [] }).Add(value => value.Players, Array.Empty<PlayerSummary>()));
        coverage.Markup.ShouldContain("0 players");
        coverage.Markup.ShouldContain("No total target set");
        coverage.FindAll(".coverage-list li").ShouldBeEmpty();
    }

    [Fact]
    public async Task TeamTargetsPreserveDraftOnConflictAndSubmitObservedRevisionAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data, ClubRole.Administrator);
        var team = data.Teams[0] with { RosterTarget = 12, PositionTargets = [new("Keeper", 1)] };
        gateway.GetTeamAsync(_clubId, team.Id, null, Arg.Any<CancellationToken>()).Returns(new TeamDetail(team, data.Season, [], []));
        gateway.SaveTeamTargetsAsync(_clubId, team.Id, Arg.Any<TeamTargetsInput>(), Arg.Any<CancellationToken>()).Returns(new SportReply(SportReplyKind.Conflict, "Team changed. Reload before saving."));
        var page = context.Render<TeamTargets>(parameters => parameters.Add(value => value.ClubId, _clubId).Add(value => value.TeamId, team.Id));
        await page.Find("#target-total").ChangeAsync("0");
        await page.Find(".target-row input[type='number']").ChangeAsync("2");
        await page.Find("#target-total").Closest("form")!.SubmitAsync();
        await gateway.Received(1).SaveTeamTargetsAsync(_clubId, team.Id, Arg.Is<TeamTargetsInput>(value => value.Revision == team.Revision && value.RosterTarget == 0 && value.Positions.Count == 1 && value.Positions[0].Players == 2), Arg.Any<CancellationToken>());
        page.Markup.ShouldContain("Team changed");
        page.Find("#target-total").GetAttribute("value").ShouldBe("0");
        page.Find(".target-row input[type='number']").GetAttribute("value").ShouldBe("2");
        await page.Find(".target-row button").ClickAsync();
        page.FindAll(".target-row").ShouldBeEmpty();
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Reload saved targets", StringComparison.Ordinal)).ClickAsync();
        page.Find("#target-total").GetAttribute("value").ShouldBe("12");
        page.FindAll(".target-row").Count.ShouldBe(1);
    }

    [Fact]
    public async Task CoachesCannotLoadTargetEditingPageAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        var page = context.Render<TeamTargets>(parameters => parameters.Add(value => value.ClubId, _clubId).Add(value => value.TeamId, data.Teams[0].Id));
        page.FindAll("#target-total").ShouldBeEmpty();
        await gateway.DidNotReceive().GetTeamAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PlanningFiltersIntersectWithoutChangingSelectedNotebookOrDraftAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        data = data with { Roster = [data.Roster[0] with { Player = data.Roster[0].Player with { SecondaryPosition = "Keeper" }, HasObservations = true }, data.Roster[1]] };
        var gateway = Configure(context, data);
        var previous = data.Season with { Id = Guid.NewGuid(), Name = "Previous season" };
        var previousTeam = data.Teams[0] with { Id = Guid.NewGuid() };
        gateway.GetOverviewAsync(_clubId, Arg.Any<CancellationToken>()).Returns(new SportOverview([data.Season, previous], data.Teams, []));
        gateway.GetSeasonReviewAsync(_clubId, previous.Id, Arg.Any<CancellationToken>()).Returns(new SeasonReview(previous, [new(previousTeam, [data.Roster[1].Player])], [], [], 1));
        gateway.GetNotebookAsync(_clubId, _tryoutId, data.Roster[0].Player.Id, Arg.Any<CancellationToken>()).Returns(new PlayerNotebook(data.Roster[0].Player.Id, false,
            [new(Guid.NewGuid(), data.Roster[0].Player.Id, "Current observation", "Coach", DateTimeOffset.UtcNow, null, true)], [], []));
        var page = Render(context);
        await page.Find("#player-note").InputAsync("Unsubmitted draft");
        await page.Find("#roster-position").ChangeAsync("Keeper");
        page.FindAll(".roster-record").ShouldHaveSingleItem().TextContent.ShouldContain("Jordan Rivera");
        await page.Find("#roster-observations").ChangeAsync("missing");
        page.FindAll(".roster-record").ShouldBeEmpty();
        await page.Find("#roster-position").ChangeAsync("");
        page.FindAll(".roster-record").ShouldHaveSingleItem().TextContent.ShouldContain("Casey Chen");
        await page.Find("#roster-previous-season").ChangeAsync(previous.Id.ToString());
        await page.Find("#roster-previous-team").ChangeAsync(previousTeam.Id.ToString());
        page.FindAll(".roster-record").ShouldHaveSingleItem().TextContent.ShouldContain("Casey Chen");
        await page.Find("#roster-previous-team").ChangeAsync("none");
        page.FindAll(".roster-record").ShouldBeEmpty();
        page.Find("#player-note").GetAttribute("value").ShouldBe("Unsubmitted draft");
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Clear player filters", StringComparison.Ordinal)).ClickAsync();
        page.FindAll(".roster-record").Count.ShouldBe(2);
        await gateway.Received(1).GetNotebookAsync(_clubId, _tryoutId, data.Roster[0].Player.Id, Arg.Any<CancellationToken>());
    }
}
