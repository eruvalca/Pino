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
    public async Task PreviousTeamActionSavesObservedDecisionAndKeepsUnsubmittedNoteAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var previous = new PreviousPlacementSummary(Guid.NewGuid(), "Last season", data.Teams[0].Id, data.Teams[0].Name);
        data = data with { Roster = [data.Roster[0] with { PreviousPlacement = previous }, data.Roster[1]] };
        var gateway = Configure(context, data);
        gateway.DecideAsync(_clubId, _tryoutId, Arg.Any<DecisionInput>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var input = call.Arg<DecisionInput>();
            var saved = data.Roster[0] with { Decision = input.Kind, DecisionTeamId = input.TeamId, CurrentTeamId = input.TeamId, Revision = 4, PlacementRevision = 9 };
            gateway.GetTryoutAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>()).Returns(data with { Roster = [saved, data.Roster[1]] });
            return new SportReply(SportReplyKind.Saved, "Decision saved.");
        });
        var page = Render(context);
        await page.Find("#player-note").InputAsync("Keep this unfinished observation");
        await page.Find(".previous-placement button").ClickAsync();
        await gateway.Received(1).DecideAsync(_clubId, _tryoutId, Arg.Is<DecisionInput>(value => value.PlayerId == data.Roster[0].Player.Id && value.Kind == DecisionKind.Placed && value.TeamId == previous.TeamId && value.Revision == 3 && value.PlacementRevision == 8), Arg.Any<CancellationToken>());
        page.Find(".previous-placement").TextContent.ShouldContain("Same team recorded");
        page.Find("#player-note").GetAttribute("value").ShouldBe("Keep this unfinished observation");
        await gateway.DidNotReceive().AddNoteAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<NoteInput>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true, false, 2030)]
    [InlineData(false, true, 2030)]
    [InlineData(false, false, 2031)]
    public async Task PreviousTeamActionDoesNotOfferArchivedExcludedOrIneligibleTeamsAsync(bool archived, bool excluded, int year)
    {
        await using var context = new BunitContext();
        var data = Data();
        var team = data.Teams[0] with { Archived = archived, Excluded = excluded, GraduationYear = year };
        var entry = data.Roster[0] with { PreviousPlacement = new(Guid.NewGuid(), "Last season", team.Id, team.Name) };
        var page = context.Render<PreviousPlacementAction>(parameters => parameters.Add(value => value.Entry, entry).Add(value => value.Team, team).Add(value => value.Place, () => throw new InvalidOperationException("Unavailable action executed")));
        page.FindAll("button").ShouldBeEmpty();
        page.Find("strong").TextContent.ShouldBe(team.Name);
    }

    [Fact]
    public async Task GroupExclusionRequiresReviewAndReasonAndRetainsOperationAfterTransportFailureAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        gateway.GetEnrollmentsAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>()).Returns(data.Roster.Select(value => new EnrollmentDetail(value, false, [])).ToArray());
        var attempts = new List<BulkEnrollmentChangeInput>();
        gateway.ChangeEnrollmentsAsync(_clubId, _tryoutId, Arg.Any<BulkEnrollmentChangeInput>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            attempts.Add(call.Arg<BulkEnrollmentChangeInput>());
            return attempts.Count == 1 ? Task.FromException<SportingBatchReport>(new HttpRequestException("Lost response")) : Task.FromResult(new SportingBatchReport(SportReplyKind.Saved, "1 applied, 1 skipped", 1, 1, [new(data.Roster[1].Player.Id, "Placement changed", false)]));
        });
        var page = context.Render<TryoutEnrollments>(parameters => parameters.Add(value => value.ClubId, _clubId).Add(value => value.TryoutId, _tryoutId));
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Select all matching", StringComparison.Ordinal)).ClickAsync();
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Review players to exclude", StringComparison.Ordinal)).ClickAsync();
        page.FindAll("button").Single(value => string.Equals(value.TextContent, "Confirm exclusion", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeTrue();
        attempts.ShouldBeEmpty();
        await page.Find("#batch-enrollment-reason").InputAsync("These players attend the later tryout");
        await page.Find("#batch-enrollment-reason").Closest("form")!.SubmitAsync();
        page.Find("#batch-enrollment-reason").GetAttribute("value").ShouldBe("These players attend the later tryout");
        await page.Find("#batch-enrollment-reason").Closest("form")!.SubmitAsync();
        attempts.Count.ShouldBe(2);
        attempts[1].OperationId.ShouldBe(attempts[0].OperationId);
        attempts[0].Remove.ShouldBeTrue();
        attempts[0].Reason.ShouldBe("These players attend the later tryout");
        attempts[0].Players.ShouldBe(data.Roster.Select(value => new EnrollmentChangeSelection(value.Player.Id, value.Revision, value.PlacementRevision)).ToArray());
        page.Find(".notice[data-kind='warning']").TextContent.ShouldContain("1 applied, 1 skipped");
        page.Markup.ShouldContain("Placement changed");
    }

    [Fact]
    public async Task CoachCannotOpenTeamAvailabilityAdministrationAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data, ClubRole.Coach);
        var page = context.Render<TeamAvailability>(parameters => parameters.Add(value => value.ClubId, _clubId).Add(value => value.SeasonId, data.Season.Id));
        page.Find("h1").TextContent.ShouldBe("Workspace unavailable");
        await gateway.DidNotReceive().GetTeamAvailabilityAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>());
    }
}
