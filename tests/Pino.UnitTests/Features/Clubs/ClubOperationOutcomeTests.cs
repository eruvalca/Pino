using System.Diagnostics.CodeAnalysis;
using Pino.Features.Clubs.Models;
using Pino.SharedKernel.Clubs;
using Shouldly;
using Xunit;

namespace Pino.UnitTests.Features.Clubs;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "xUnit requires public test classes for discovery.")]
public sealed class ClubOperationOutcomeTests
{
    [Fact]
    public void EveryOutcomePreservesItsKindAndMessage()
    {
        ClubOperationOutcome saved = new ClubOperationOutcome.Saved("Profile saved.");
        ClubOperationOutcome invalid = new ClubOperationOutcome.Invalid("Choose a state.");
        ClubOperationOutcome forbidden = new ClubOperationOutcome.Forbidden("Access ended.");
        ClubOperationOutcome conflict = new ClubOperationOutcome.Conflict("Role changed.");
        saved.ToReply().ShouldBe(new(ClubReplyKind.Saved, "Profile saved."));
        invalid.ToReply().ShouldBe(new(ClubReplyKind.Invalid, "Choose a state."));
        forbidden.ToReply().ShouldBe(new(ClubReplyKind.Forbidden, "Access ended."));
        conflict.ToReply().ShouldBe(new(ClubReplyKind.Conflict, "Role changed."));
        saved.ToReply().Succeeded.ShouldBeTrue();
        invalid.ToReply().Succeeded.ShouldBeFalse();
        forbidden.ToReply().Succeeded.ShouldBeFalse();
        conflict.ToReply().Succeeded.ShouldBeFalse();
    }
}
