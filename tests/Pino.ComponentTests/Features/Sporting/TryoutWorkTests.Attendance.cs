using Bunit;
using NSubstitute;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Sporting;

public sealed partial class TryoutWorkTests
{
    [Fact]
    public async Task AttendanceFiltersKeepSelectedPlayerAndUnsavedNoteAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        gateway.GetAttendanceAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>()).Returns(new AttendanceSummary[] { new(data.Roster[0].Player.Id, AttendanceKind.Absent, 3, "Avery Coach", DateTimeOffset.UtcNow) });
        var page = Render(context);
        await page.Find("#player-note").InputAsync("Keep this note while filtering.");
        page.Find(".notebook-heading h2").TextContent.ShouldBe("Jordan Rivera");
        page.Find("#player-note").GetAttribute("value").ShouldBe("Keep this note while filtering.");
        await page.Find("#roster-attendance").ChangeAsync("Absent");
        page.FindAll(".roster-record").ShouldHaveSingleItem().TextContent.ShouldContain("Jordan Rivera");
        await page.Find("#roster-attendance").ChangeAsync("Present");
        page.FindAll(".roster-record").ShouldBeEmpty();
        page.Find("#player-note").GetAttribute("value").ShouldBe("Keep this note while filtering.");
    }

    [Fact]
    public async Task AttendanceSubmitsObservedRevisionWithoutChangingDecisionOrUnsavedNoteAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        var player = data.Roster[0].Player.Id;
        var saved = new AttendanceSummary(player, AttendanceKind.Absent, 4, "Avery Coach", DateTimeOffset.UtcNow);
        gateway.GetAttendanceAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>()).Returns(new[] { saved });
        gateway.SaveAttendanceAsync(_clubId, _tryoutId, Arg.Any<AttendanceInput>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            gateway.GetAttendanceAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>()).Returns(new[] { saved with { Kind = AttendanceKind.Present, Revision = 5 } });
            return new SportReply(SportReplyKind.Saved, "Attendance saved; selection unchanged.");
        });
        var page = Render(context);
        await page.Find("#player-note").InputAsync("Keep my draft.");
        await page.FindAll("button").Single(button => string.Equals(button.TextContent, "Mark present", StringComparison.Ordinal)).ClickAsync();
        await gateway.Received(1).SaveAttendanceAsync(_clubId, _tryoutId, new(player, AttendanceKind.Present, 4), Arg.Any<CancellationToken>());
        await gateway.DidNotReceive().DecideAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DecisionInput>(), Arg.Any<CancellationToken>());
        page.Find("#player-note").GetAttribute("value").ShouldBe("Keep my draft.");
        page.Find(".decision-badge").TextContent.ShouldBe("Awaiting decision");
        page.FindAll("button").Single(button => string.Equals(button.TextContent, "Mark present", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeTrue();
    }
}
