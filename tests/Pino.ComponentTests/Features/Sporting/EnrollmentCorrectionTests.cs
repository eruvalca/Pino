using System.Diagnostics.CodeAnalysis;
using Bunit;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Components;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class EnrollmentCorrectionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReasonIsRequiredAndRetryKeepsOperationAndObservedRevisionsAsync(bool removed)
    {
        await using var context = new BunitContext();
        var player = new PlayerSummary(Guid.NewGuid(), "SYN-1", "Jordan", "Rivera", 2030, "", "", false, 1, null);
        var enrollment = new EnrollmentDetail(new(player, "17", DecisionKind.Placed, Guid.NewGuid(), 7, Guid.NewGuid(), Guid.NewGuid(), 9), removed, []);
        var submissions = new List<EnrollmentChangeInput>();
        var component = context.Render<EnrollmentCorrection>(parameters => parameters.Add(value => value.Enrollment, enrollment)
            .Add(value => value.Submitted, submissions.Add));
        await component.Find("form").SubmitAsync();
        submissions.ShouldBeEmpty();
        component.Find("button[type=submit]").HasAttribute("disabled").ShouldBeTrue();
        await component.Find("#enrollment-reason").InputAsync("Wrong tryout selected.");
        if (removed) { await component.Find("#restore-bib").ChangeAsync("28"); }
        await component.Find("form").SubmitAsync();
        await component.Find("form").SubmitAsync();
        submissions.Count.ShouldBe(2);
        submissions[1].ShouldBe(submissions[0]);
        submissions[0].OperationId.ShouldNotBe(Guid.Empty);
        submissions[0].Revision.ShouldBe(7);
        submissions[0].PlacementRevision.ShouldBe(9);
        submissions[0].Remove.ShouldBe(!removed);
        submissions[0].Bib.ShouldBe(removed ? "28" : "17");
        enrollment.Entry.Bib.ShouldBe("17");
        component.Render(parameters => parameters.Add(value => value.Disabled, true));
        await component.Find("form").SubmitAsync();
        submissions.Count.ShouldBe(2);
    }
}
