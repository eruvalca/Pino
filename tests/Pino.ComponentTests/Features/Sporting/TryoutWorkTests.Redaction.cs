using Bunit;
using NSubstitute;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Sporting;

public sealed partial class TryoutWorkTests
{
    [Fact]
    public async Task AdministratorRedactionRequiresReasonAndConfirmationThenClearsCorrectionDraftAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var note = new NoteSummary(Guid.NewGuid(), data.Roster[0].Player.Id, "Remove sensitive text", "Avery Coach", DateTimeOffset.UtcNow, null, true);
        var gateway = Configure(context, data, ClubRole.Administrator);
        gateway.GetNotebookAsync(_clubId, _tryoutId, note.PlayerId, Arg.Any<CancellationToken>()).Returns(new PlayerNotebook(note.PlayerId, false, [note], [], []));
        gateway.RedactNoteAsync(_clubId, _tryoutId, Arg.Any<RedactNoteInput>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            gateway.GetNotebookAsync(_clubId, _tryoutId, note.PlayerId, Arg.Any<CancellationToken>()).Returns(new PlayerNotebook(note.PlayerId, false,
                [note with { Text = "", CanCorrect = false, RedactedAt = DateTimeOffset.UtcNow, RedactedBy = "Avery Admin", RedactionReason = "Personal content" }], [], []));
            return new SportReply(SportReplyKind.Saved, "Observation redacted.");
        });
        var page = Render(context);
        await page.FindAll("button").Single(button => string.Equals(button.TextContent, "Correct note", StringComparison.Ordinal)).ClickAsync();
        page.Find("#player-note").GetAttribute("value").ShouldBe(note.Text);
        await page.FindAll("button").Single(button => string.Equals(button.TextContent, "Review text removal", StringComparison.Ordinal)).ClickAsync();
        page.Find("#redaction-reason").Closest("form")!.QuerySelector("button[type=submit]")!.HasAttribute("disabled").ShouldBeTrue();
        await page.Find("#redaction-reason").InputAsync("Personal content");
        page.Find("#redaction-reason").Closest("form")!.QuerySelector("button[type=submit]")!.HasAttribute("disabled").ShouldBeTrue();
        await page.Find("#redaction-reason").Closest("form")!.QuerySelector("input[type=checkbox]")!.ChangeAsync(true);
        await page.Find("#redaction-reason").Closest("form")!.SubmitAsync();
        await gateway.Received(1).RedactNoteAsync(_clubId, _tryoutId, Arg.Is<RedactNoteInput>(input => input.NoteId == note.Id && input.OperationId != Guid.Empty && input.Reason == "Personal content" && input.ConfirmAllVersions), Arg.Any<CancellationToken>());
        await page.WaitForAssertionAsync(() => page.Markup.ShouldContain("Private note text removed."));
        page.Markup.ShouldNotContain(note.Text);
        page.Find("#player-note").GetAttribute("value").ShouldBeEmpty();
        page.Markup.ShouldContain("Reason: Personal content");
        page.Markup.ShouldContain("Avery Admin");
    }

    [Fact]
    public async Task CoachCannotSeeRedactionActionAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        gateway.GetNotebookAsync(_clubId, _tryoutId, data.Roster[0].Player.Id, Arg.Any<CancellationToken>()).Returns(new PlayerNotebook(data.Roster[0].Player.Id, false,
            [new(Guid.NewGuid(), data.Roster[0].Player.Id, "Shared observation", "Coach", DateTimeOffset.UtcNow, null, true)], [], []));
        var page = Render(context);
        page.Markup.ShouldContain("Correct note");
        page.Markup.ShouldNotContain("Review text removal");
    }
}
