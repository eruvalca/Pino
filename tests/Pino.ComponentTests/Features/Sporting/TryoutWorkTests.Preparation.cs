using Bunit;
using NSubstitute;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Pages;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Sporting;

public sealed partial class TryoutWorkTests
{
    [Fact]
    public async Task BulkEnrollmentReviewsSelectionAndRetriesSameObservedBatchAfterTransportFailureAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data, ClubRole.Administrator);
        var player = data.Roster[0].Player;
        var enrolled = data.Roster[1].Player;
        var otherYear = player with { Id = Guid.NewGuid(), FirstName = "Taylor", GraduationYear = 2031 };
        gateway.GetEnrollmentCandidatesAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>()).Returns(new EnrollmentCandidate[] { new(player, false, false), new(enrolled, true, true), new(otherYear, false, false) });
        var attempts = new List<BulkEnrollmentInput>();
        gateway.EnrollBulkAsync(_clubId, _tryoutId, Arg.Any<BulkEnrollmentInput>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            attempts.Add(call.Arg<BulkEnrollmentInput>());
            return attempts.Count == 1 ? Task.FromException<SportingBatchReport>(new HttpRequestException("Lost acknowledgement")) : Task.FromResult(new SportingBatchReport(SportReplyKind.Saved, "Batch completed: 0 applied, 1 skipped.", 0, 1, [new(player.Id, "Player changed after review.", false)]));
        });
        var page = context.Render<BulkEnrollment>(parameters => parameters.Add(value => value.ClubId, _clubId).Add(value => value.TryoutId, _tryoutId));
        await page.Find("#bulk-year").ChangeAsync("2030");
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Select all available matches", StringComparison.Ordinal)).ClickAsync();
        page.FindAll("input[type='checkbox']:checked").Count.ShouldBe(1);
        page.FindAll(".record-list input[type='checkbox']").Count.ShouldBe(1);
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Review selected players", StringComparison.Ordinal)).ClickAsync();
        attempts.ShouldBeEmpty();
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Add reviewed players", StringComparison.Ordinal)).ClickAsync();
        page.Markup.ShouldContain("unsaved input is still here");
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Add reviewed players", StringComparison.Ordinal)).ClickAsync();
        attempts.Count.ShouldBe(2);
        attempts[1].ShouldBe(attempts[0]);
        attempts[0].Players.ShouldHaveSingleItem().ShouldBe(new(player.Id, player.Revision));
        page.Find(".notice[data-kind='warning']").TextContent.ShouldContain("0 applied, 1 skipped");
        page.Markup.ShouldContain("Player changed after review.");
    }

    [Fact]
    public async Task ReturningReviewKeepsObservedVersionsAndExcludesPlayersNeedingAttentionAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data, ClubRole.Administrator);
        var source = data.Season with { Id = Guid.NewGuid(), Name = "Previous season", StartsOn = new(2026, 1, 1), EndsOn = new(2026, 12, 31) };
        gateway.GetOverviewAsync(_clubId, Arg.Any<CancellationToken>()).Returns(new SportOverview([data.Season, source], data.Teams, [data.Tryout]));
        var selection = new ReturningSelection(data.Roster[0].Player.Id, data.Teams[0].Id, data.Teams[0].Id, 4, 5, 6, 7, 8);
        gateway.GetReturningPlayersAsync(_clubId, _tryoutId, source.Id, Arg.Any<CancellationToken>()).Returns(new ReturningPlayerReview[]
        {
            new(data.Roster[0].Player, "Previous Blue", "Current Blue", selection, "Ready"),
            new(data.Roster[1].Player, "Previous Silver", null, null, "Team is excluded from this tryout."),
        });
        gateway.PlaceReturningPlayersAsync(_clubId, _tryoutId, Arg.Any<ReturningPlacementInput>(), Arg.Any<CancellationToken>()).Returns(new SportingBatchReport(SportReplyKind.Saved, "Completed", 1, 0, []));
        var page = context.Render<ReturningPlayers>(parameters => parameters.Add(value => value.ClubId, _clubId).Add(value => value.TryoutId, _tryoutId));
        await page.Find("#returning-source").ChangeAsync(source.Id.ToString());
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Continue to players", StringComparison.Ordinal)).ClickAsync();
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Select ready matches", StringComparison.Ordinal)).ClickAsync();
        page.FindAll("input[type='checkbox']:checked").Count.ShouldBe(1);
        page.Markup.ShouldContain("Team is excluded from this tryout.");
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Review selected placements", StringComparison.Ordinal)).ClickAsync();
        await gateway.DidNotReceive().PlaceReturningPlayersAsync(_clubId, _tryoutId, Arg.Any<ReturningPlacementInput>(), Arg.Any<CancellationToken>());
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Place reviewed players", StringComparison.Ordinal)).ClickAsync();
        await gateway.Received(1).PlaceReturningPlayersAsync(_clubId, _tryoutId, Arg.Is<ReturningPlacementInput>(input => input.SourceSeasonId == source.Id && input.Players.Count == 1 && input.Players[0] == selection && input.OperationId != Guid.Empty), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TeamAvailabilitySubmitsObservedScopeRevisionAndReportsConflictAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data, ClubRole.Administrator);
        gateway.GetTeamAvailabilityAsync(_clubId, data.Season.Id, null, Arg.Any<CancellationToken>()).Returns(new TeamAvailabilitySummary[] { new(data.Teams[0], false, 7, false, 0) });
        gateway.SaveTeamAvailabilityAsync(_clubId, data.Season.Id, null, Arg.Any<TeamAvailabilityInput>(), Arg.Any<CancellationToken>()).Returns(new SportReply(SportReplyKind.Conflict, "Availability changed"));
        var page = context.Render<TeamAvailability>(parameters => parameters.Add(value => value.ClubId, _clubId).Add(value => value.SeasonId, data.Season.Id));
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Exclude team", StringComparison.Ordinal)).ClickAsync();
        await gateway.Received(1).SaveTeamAvailabilityAsync(_clubId, data.Season.Id, null, new TeamAvailabilityInput(data.Teams[0].Id, true, 7), Arg.Any<CancellationToken>());
        page.Find(".notice[data-kind='error']").TextContent.ShouldBe("Availability changed");
        page.Markup.ShouldContain("Included");
    }
}
