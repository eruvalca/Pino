using System.Diagnostics.CodeAnalysis;
using Pino.Features.Clubs.Services;
using Pino.SharedKernel.Clubs;
using Shouldly;
using Xunit;

namespace Pino.UnitTests.Features.Clubs;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "xUnit requires public test classes for discovery.")]
public sealed class ClubRulesTests
{
    [Theory]
    [InlineData("", "Diaz", false)]
    [InlineData("   ", "Diaz", false)]
    [InlineData("Ana", "", false)]
    [InlineData("Ana", "Diaz", true)]
    public void ProfileRequiresBothNames(string first, string last, bool expected) =>
        ClubRules.ValidProfile(new() { FirstName = first, LastName = last }).ShouldBe(expected);

    [Theory]
    [InlineData(80, true)]
    [InlineData(81, false)]
    public void ProfileEnforcesEachNameLimit(int length, bool expected)
    {
        ClubRules.ValidProfile(new() { FirstName = new('A', length), LastName = "Diaz" }).ShouldBe(expected);
        ClubRules.ValidProfile(new() { FirstName = "Ana", LastName = new('D', length) }).ShouldBe(expected);
    }

    [Theory]
    [InlineData("IL", true)]
    [InlineData("WY", true)]
    [InlineData("il", false)]
    [InlineData("", false)]
    [InlineData("XX", false)]
    public void ClubRequiresKnownState(string state, bool expected) =>
        ClubRules.ValidClub(new() { Name = "Northside FC", Sport = "Soccer", City = "Chicago", State = state }).ShouldBe(expected);

    [Fact]
    public void ClubRejectsEmptyOperationAndMissingOrOverlongFields()
    {
        var input = new CreateClubInput { Name = new('N', 120), Sport = new('S', 60), City = new('C', 100), State = "IL" };
        ClubRules.ValidClub(input).ShouldBeTrue();
        input.Name += "N";
        ClubRules.ValidClub(input).ShouldBeFalse();
        input.Name = "Club"; input.Sport += "S";
        ClubRules.ValidClub(input).ShouldBeFalse();
        input.Sport = "Soccer"; input.City += "C";
        ClubRules.ValidClub(input).ShouldBeFalse();
        input.City = "Chicago"; input.OperationId = Guid.Empty;
        ClubRules.ValidClub(input).ShouldBeFalse();
        input.OperationId = Guid.NewGuid(); input.Name = " ";
        ClubRules.ValidClub(input).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(ClubRole.Coach)]
    public void LastAdministratorCannotLoseAccess(ClubRole? next) =>
        ClubRules.MemberChangeError(ClubRole.Administrator, ClubRole.Administrator, next, 1)!.ShouldContain("Promote another member");

    [Theory]
    [InlineData(ClubRole.Administrator, ClubRole.Coach, 2)]
    [InlineData(ClubRole.Administrator, null, 2)]
    [InlineData(ClubRole.Coach, null, 1)]
    [InlineData(ClubRole.Coach, ClubRole.Administrator, 1)]
    [InlineData(ClubRole.Administrator, ClubRole.Administrator, 1)]
    public void ValidMembershipChangesAreAllowed(ClubRole current, ClubRole? next, int admins) =>
        ClubRules.MemberChangeError(current, current, next, admins).ShouldBeNull();

    [Fact]
    public void StaleAndInvalidRolesAreRejected()
    {
        ClubRules.MemberChangeError(ClubRole.Administrator, ClubRole.Coach, null, 2)!.ShouldContain("role changed");
        ClubRules.MemberChangeError(ClubRole.Coach, (ClubRole)99, null, 2)!.ShouldContain("valid member role");
        ClubRules.MemberChangeError(ClubRole.Coach, ClubRole.Coach, (ClubRole)99, 2)!.ShouldContain("valid member role");
    }
}
