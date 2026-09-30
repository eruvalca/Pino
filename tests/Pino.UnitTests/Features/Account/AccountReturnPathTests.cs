using System.Diagnostics.CodeAnalysis;
using Pino.Features.Account.Services;
using Shouldly;
using Xunit;

namespace Pino.UnitTests.Features.Account;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class AccountReturnPathTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://untrusted.example/")]
    [InlineData("//untrusted.example/")]
    [InlineData("/\\untrusted.example/")]
    [InlineData("javascript:alert(1)")]
    public void InvalidContinuationReturnsToClubAccess(string? path) => AccountReturnPath.Local(path).ShouldBe("/club/access");

    [Theory]
    [InlineData("/club/invitations/abc?token=secret")]
    [InlineData("/events")]
    [InlineData("Account/Manage")]
    public void LocalContinuationSurvivesEncodingIntoSignInLink(string path)
    {
        AccountReturnPath.Local(path).ShouldBe(path);
        AccountReturnPath.Link("/Account/Login", path).ShouldBe("/Account/Login?returnUrl=" + Uri.EscapeDataString(path));
    }
}
