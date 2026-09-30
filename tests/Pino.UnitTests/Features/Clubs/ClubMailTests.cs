using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Pino.Features.Clubs.Services;
using Pino.SharedKernel.Clubs;
using Shouldly;
using Xunit;

namespace Pino.UnitTests.Features.Clubs;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class ClubMailTests
{
    [Fact]
    public void QueuedBodyIsProtectedAndReceiptStartsPending()
    {
        var mail = Create("Development");
        var club = Guid.NewGuid();
        var email = mail.Compose(club, "Invited@example.test", "staff-id", "Invitation", "Private token and content");
        email.Id.ShouldNotBe(Guid.Empty);
        email.ClubId.ShouldBe(club);
        email.RecipientEmail.ShouldBe("Invited@example.test");
        email.NormalizedRecipientEmail.ShouldBe("INVITED@EXAMPLE.TEST");
        email.RecipientId.ShouldBe("staff-id");
        email.Status.ShouldBe(StaffEmailStatus.Pending);
        email.SentAt.ShouldBeNull();
        email.ProtectedBody.ShouldNotContain("Private token");
        mail.ReadBody(email).ShouldBe("Private token and content");
        email.Attempts.ShouldBe(0);
    }

    [Theory]
    [InlineData("http://localhost:1234/", "http://localhost:1234/")]
    [InlineData("https://localhost:4567/", "https://localhost:4567/")]
    public void LocalInvitationLinkContainsOnlyTheGeneratedTokenAndIdentifier(string origin, string expected)
    {
        var id = Guid.NewGuid();
        var uri = Create("Development").InvitationUrl(new(origin), id, "safe-token");
        uri.AbsoluteUri.ShouldBe($"{expected}club/invitations/{id}?token=safe-token");
    }

    [Theory]
    [InlineData("Development", "https://untrusted.example/")]
    [InlineData("Production", "https://localhost:1234/")]
    [InlineData("Development", "http://user@localhost:1234/")]
    [InlineData("Development", "https://localhost:1234/?query=bad")]
    public void RequestHostCannotChooseAnExternalInvitationDestination(string environment, string origin)
    {
        Should.Throw<InvalidOperationException>(() => Create(environment).InvitationUrl(new(origin), Guid.NewGuid(), "token"))
            .ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public void ConfiguredHttpsOriginOverridesTheRequestHost()
    {
        var uri = Create("Production", "https://pino.example/").InvitationUrl(new("https://untrusted.example/"), Guid.Empty, "token");
        uri.Host.ShouldBe("pino.example");
        uri.Scheme.ShouldBe("https");
    }

    [Fact]
    public void InvitationTokensRequireTheWholeExactSecret()
    {
        var token = InvitationTokens.Create();
        token.Length.ShouldBe(43);
        var hash = InvitationTokens.Hash(token);
        hash.Length.ShouldBe(32);
        InvitationTokens.Matches(token, hash).ShouldBeTrue();
        InvitationTokens.Matches(new string('x', 43), hash).ShouldBeFalse();
        InvitationTokens.Matches(token[..42], hash).ShouldBeFalse();
        InvitationTokens.Matches(token + "a", hash).ShouldBeFalse();
        InvitationTokens.Matches(null, hash).ShouldBeFalse();
        InvitationTokens.Matches(token, []).ShouldBeFalse();
    }

    private static ClubMail Create(string name, string? origin = null)
    {
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.EnvironmentName.Returns(name);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) { ["Pino:PublicOrigin"] = origin }).Build();
        return new(new EphemeralDataProtectionProvider(), configuration, environment, TimeProvider.System);
    }
}
