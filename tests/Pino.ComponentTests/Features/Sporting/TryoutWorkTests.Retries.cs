using Bunit;
using NSubstitute;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Sporting;

public sealed partial class TryoutWorkTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LostNoteResponseReconcilesWithoutLosingEditsOrDuplicatingAsync(bool editAfterFailure)
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        NoteInput? committed = null;
        gateway.AddNoteAsync(_clubId, _tryoutId, Arg.Any<NoteInput>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            committed = call.Arg<NoteInput>();
            gateway.GetNotebookAsync(_clubId, _tryoutId, committed.PlayerId, Arg.Any<CancellationToken>()).Returns(new PlayerNotebook(committed.PlayerId, false,
                [new(committed.Id, committed.PlayerId, committed.Text, "Avery Coach", DateTimeOffset.UtcNow, committed.CorrectsId, CanCorrect: true)], [], []));
            return Task.FromException<SportReply>(new HttpRequestException("The response was lost after commit."));
        });
        var page = Render(context);
        await page.Find("#player-note").InputAsync("Original observation.");
        await page.Find(".note-composer").SubmitAsync();
        var first = committed.ShouldNotBeNull();
        if (editAfterFailure) { await page.Find("#player-note").InputAsync("Updated observation."); }
        await page.Find(".note-composer").SubmitAsync();
        await gateway.Received(1).AddNoteAsync(_clubId, _tryoutId, Arg.Any<NoteInput>(), Arg.Any<CancellationToken>());
        if (editAfterFailure)
        {
            page.Find("#player-note").GetAttribute("value").ShouldBe("Updated observation.");
            page.Find("label[for='player-note']").TextContent.ShouldBe("Correct your note");
            page.Find(".notice").TextContent.ShouldContain("newer edits are still here");
            gateway.AddNoteAsync(_clubId, _tryoutId, Arg.Any<NoteInput>(), Arg.Any<CancellationToken>()).Returns(new SportReply(SportReplyKind.Saved, "Correction saved."));
            await page.Find(".note-composer").SubmitAsync();
            await gateway.Received(1).AddNoteAsync(_clubId, _tryoutId, Arg.Is<NoteInput>(input => input.Id != first.Id && input.CorrectsId == first.Id && input.Text == "Updated observation."), Arg.Any<CancellationToken>());
        }
        page.Find("#player-note").GetAttribute("value").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("unchanged")]
    [InlineData("outcome")]
    [InlineData("team")]
    [InlineData("reason")]
    public async Task LostDecisionResponseReconcilesAndPreservesNewerChoiceAsync(string change)
    {
        await using var context = new BunitContext();
        var data = Data();
        var alternative = new TeamSummary(Guid.NewGuid(), "Another eligible team", 2030, Archived: false, 1);
        data = data with { Teams = [.. data.Teams, alternative] };
        var gateway = Configure(context, data);
        DecisionInput? committed = null;
        gateway.DecideAsync(_clubId, _tryoutId, Arg.Any<DecisionInput>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            committed = call.Arg<DecisionInput>();
            gateway.GetTryoutAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>()).Returns(data with
            {
                Roster = [data.Roster[0] with { Decision = committed.Kind, DecisionTeamId = committed.TeamId, CurrentTeamId = committed.TeamId, Revision = 4, PlacementRevision = 9 }, data.Roster[1]],
            });
            gateway.GetNotebookAsync(_clubId, _tryoutId, committed.PlayerId, Arg.Any<CancellationToken>()).Returns(new PlayerNotebook(committed.PlayerId, false, [],
                [new(committed.OperationId, committed.PlayerId, _tryoutId, "Spring evaluation", "Spring", committed.Kind, Team: null, "Avery Coach", DateTimeOffset.UtcNow, committed.Reason, committed.TeamId)], []));
            return Task.FromException<SportReply>(new HttpRequestException("The response was lost after commit."));
        });
        var page = Render(context);
        await page.Find("#decision-kind").ChangeAsync("Placed");
        await page.Find("#decision-team").ChangeAsync(data.Teams[0].Id.ToString());
        await page.Find("#decision-reason").ChangeAsync("Original reason.");
        await page.Find("#decision-kind").Closest("form")!.SubmitAsync();
        var first = committed.ShouldNotBeNull();
        var editAfterFailure = !string.Equals(change, "unchanged", StringComparison.Ordinal);
        if (string.Equals(change, "outcome", StringComparison.Ordinal)) { await page.Find("#decision-kind").ChangeAsync("NotSelected"); }
        if (string.Equals(change, "team", StringComparison.Ordinal)) { await page.Find("#decision-team").ChangeAsync(alternative.Id.ToString()); }
        if (string.Equals(change, "reason", StringComparison.Ordinal)) { await page.Find("#decision-reason").ChangeAsync("Updated reason."); }
        await page.Find("#decision-kind").Closest("form")!.SubmitAsync();
        await gateway.Received(1).DecideAsync(_clubId, _tryoutId, Arg.Any<DecisionInput>(), Arg.Any<CancellationToken>());
        if (editAfterFailure)
        {
            var reason = string.Equals(change, "reason", StringComparison.Ordinal) ? "Updated reason." : "Original reason.";
            var kind = string.Equals(change, "outcome", StringComparison.Ordinal) ? DecisionKind.NotSelected : DecisionKind.Placed;
            Guid? teamId = string.Equals(change, "team", StringComparison.Ordinal) ? alternative.Id : data.Teams[0].Id;
            if (kind == DecisionKind.NotSelected) { teamId = null; }
            page.Find("#decision-reason").GetAttribute("value").ShouldBe(reason);
            gateway.DecideAsync(_clubId, _tryoutId, Arg.Any<DecisionInput>(), Arg.Any<CancellationToken>()).Returns(new SportReply(SportReplyKind.Saved, "Decision saved."));
            await page.Find("#decision-kind").Closest("form")!.SubmitAsync();
            await gateway.Received(1).DecideAsync(_clubId, _tryoutId, Arg.Is<DecisionInput>(input => input.OperationId != first.OperationId && input.Kind == kind && input.TeamId == teamId && string.Equals(input.Reason, reason, StringComparison.Ordinal) && input.Revision == 4 && input.PlacementRevision == 9), Arg.Any<CancellationToken>());
        }
        page.Find("#decision-reason").GetAttribute("value").ShouldBeEmpty();
    }

    [Fact]
    public async Task FailedReconciliationPreservesDraftAndDoesNotResubmitAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context, Data());
        gateway.AddNoteAsync(_clubId, _tryoutId, Arg.Any<NoteInput>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<SportReply>(new HttpRequestException("offline")));
        var page = Render(context);
        await page.Find("#player-note").InputAsync("Original observation.");
        await page.Find(".note-composer").SubmitAsync();
        gateway.GetNotebookAsync(_clubId, _tryoutId, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<PlayerNotebook>(new HttpRequestException("still offline")));
        await page.Find("#player-note").InputAsync("Keep my edits.");
        await page.Find(".note-composer").SubmitAsync();
        page.Find("#player-note").GetAttribute("value").ShouldBe("Keep my edits.");
        await gateway.Received(1).AddNoteAsync(_clubId, _tryoutId, Arg.Any<NoteInput>(), Arg.Any<CancellationToken>());
    }
}
