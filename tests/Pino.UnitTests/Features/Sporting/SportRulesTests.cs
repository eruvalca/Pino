using System.Diagnostics.CodeAnalysis;
using Pino.Features.Sporting.Services;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.UnitTests.Features.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class SportRulesTests
{
    [Theory]
    [InlineData(2029, 2030, false)]
    [InlineData(2030, 2030, true)]
    [InlineData(2031, 2030, true)]
    public void EligibilityHonorsInclusiveGraduationThreshold(int player, int team, bool expected) =>
        SportRules.Eligible(player, team).ShouldBe(expected);

    [Theory]
    [InlineData("PLAYER-01", true)]
    [InlineData("p_01", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("p/01", false)]
    [InlineData("p 01", false)]
    public void ReferencesRejectAmbiguousOrUnsafeCharacters(string reference, bool expected) =>
        SportRules.ValidReference(reference).ShouldBe(expected);

    [Theory]
    [InlineData(1999, false)]
    [InlineData(2000, true)]
    [InlineData(2100, true)]
    [InlineData(2101, false)]
    public void PlayerYearsHaveBoundedRange(int year, bool expected) =>
        SportRules.ValidPlayer(new() { FirstName = "Avery", LastName = "Morgan", PlayerReference = "A-1", GraduationYear = year }).ShouldBe(expected);

    [Fact]
    public void PlayerValidationRequiresIdentityAndChecksOptionalFields()
    {
        var player = new PlayerInput { FirstName = "Avery", LastName = "Morgan", PlayerReference = new('A', 40), GraduationYear = 2030 };
        SportRules.ValidPlayer(player).ShouldBeTrue();
        player.PlayerReference += "A"; SportRules.ValidPlayer(player).ShouldBeFalse();
        player.PlayerReference = "A-1"; player.FirstName = " "; SportRules.ValidPlayer(player).ShouldBeFalse();
        player.FirstName = new('A', 81); SportRules.ValidPlayer(player).ShouldBeFalse();
        player.FirstName = "Avery"; player.ContactEmail = "invalid"; SportRules.ValidPlayer(player).ShouldBeFalse();
        player.ContactEmail = "parent@example.test"; SportRules.ValidPlayer(player).ShouldBeTrue();
        player.ContactEmail = " "; SportRules.ValidPlayer(player).ShouldBeTrue();
        player.ContactEmail.ShouldBeNull();
        player.Id = Guid.Empty; SportRules.ValidPlayer(player).ShouldBeFalse();
    }

    [Theory]
    [InlineData(DecisionKind.Awaiting, false, true)]
    [InlineData(DecisionKind.Withdrawn, false, true)]
    [InlineData(DecisionKind.NotSelected, false, true)]
    [InlineData(DecisionKind.Placed, true, true)]
    [InlineData(DecisionKind.Placed, false, false)]
    [InlineData(DecisionKind.Withdrawn, true, false)]
    [InlineData((DecisionKind)99, false, false)]
    public void DecisionsRequireExactlyThePayloadForTheirOutcome(DecisionKind kind, bool hasTeam, bool expected) =>
        SportRules.ValidDecision(new(Guid.NewGuid(), Guid.NewGuid(), kind, hasTeam ? Guid.NewGuid() : null, 1, 0, "")).ShouldBe(expected);

    [Fact]
    public void DecisionsBoundReasonAndRequireRetryIdentity()
    {
        var input = new DecisionInput(Guid.NewGuid(), Guid.NewGuid(), DecisionKind.Awaiting, TeamId: null, 1, 0, new('a', 1000));
        SportRules.ValidDecision(input).ShouldBeTrue();
        SportRules.ValidDecision(input with { Reason = new('a', 1001) }).ShouldBeFalse();
        SportRules.ValidDecision(input with { OperationId = Guid.Empty }).ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, 0, false)]
    [InlineData(1, 0, false)]
    [InlineData(1, 1, true)]
    [InlineData(16, 15, false)]
    [InlineData(16, 16, true)]
    public void CompletionRequiresNonemptyRosterWithEveryDecision(int players, int decisions, bool complete) =>
        new TryoutSummary(Guid.NewGuid(), Guid.NewGuid(), "Tryout", new(2027, 4, 1), "", players, decisions, 1).Complete.ShouldBe(complete);

    [Fact]
    public void ReferenceNormalizationAndSearchEscapingAreLiteral()
    {
        SportRules.Reference(" ab-1 ").ShouldBe("AB-1");
        SportRules.EscapeLike(@"A%_\B").ShouldBe(@"A\%\_\\B");
    }
}
