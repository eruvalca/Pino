using Bunit;
using NSubstitute;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Sporting;

public sealed partial class TryoutWorkTests
{
    [Fact]
    public async Task DidNotAttendIsAnExplicitDecisionWithoutATeamAndPreservesUnsavedNoteAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        gateway.DecideAsync(_clubId, _tryoutId, Arg.Any<DecisionInput>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var input = call.ArgAt<DecisionInput>(2);
            gateway.GetTryoutAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>()).Returns(data with
            {
                Roster = [data.Roster[0] with { Decision = input.Kind, Revision = 4 }, data.Roster[1]],
            });
            return new SportReply(SportReplyKind.Saved, "Decision saved.");
        });
        var page = Render(context);
        page.FindAll("#roster-attendance, .attendance-actions").ShouldBeEmpty();
        await page.Find("#player-note").InputAsync("Keep this unfinished note.");
        await page.Find("#decision-kind").ChangeAsync("Placed");
        await page.Find("#decision-team").ChangeAsync(data.Teams[0].Id.ToString());
        await page.Find("#decision-kind").ChangeAsync("DidNotAttend");
        page.FindAll("#decision-team").ShouldBeEmpty();
        await gateway.DidNotReceive().DecideAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DecisionInput>(), Arg.Any<CancellationToken>());
        await page.Find("#decision-kind").Closest("form")!.SubmitAsync();
        await gateway.Received(1).DecideAsync(_clubId, _tryoutId, Arg.Is<DecisionInput>(input =>
            input.PlayerId == data.Roster[0].Player.Id && input.Kind == DecisionKind.DidNotAttend && input.TeamId == null &&
            input.Revision == 3 && input.PlacementRevision == 8 && input.OperationId != Guid.Empty), Arg.Any<CancellationToken>());
        page.Find(".decision-badge").TextContent.ShouldBe("Did not attend");
        page.Find("#player-note").GetAttribute("value").ShouldBe("Keep this unfinished note.");
        await page.Find("#roster-decision").ChangeAsync("DidNotAttend");
        page.FindAll(".roster-record").ShouldHaveSingleItem().TextContent.ShouldContain("Jordan Rivera");
    }
}
