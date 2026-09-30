using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using NSubstitute;
using Pino.Data;
using Pino.Features.Account.Services;
using Pino.Services.Mail;
using Shouldly;
using Xunit;

namespace Pino.UnitTests.Features.Account;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class AccountProtectionTests
{
    [Fact]
    public void EmailCooldownHourlyLimitAndExpiryUseNormalizedRecipientAndPurpose()
    {
        var clock = Substitute.For<TimeProvider>();
        var now = new DateTimeOffset(2027, 1, 1, 12, 0, 0, TimeSpan.Zero);
        clock.GetUtcNow().Returns(_ => now);
        var limits = new AccountEmailLimits(clock);
        limits.TryReserve(" Coach@example.test ", "confirmation").ShouldBeTrue();
        limits.TryReserve("coach@EXAMPLE.test", "confirmation").ShouldBeFalse();
        limits.TryReserve("coach@example.test", "reset").ShouldBeTrue();
        now = now.AddSeconds(59);
        limits.TryReserve("coach@example.test", "confirmation").ShouldBeFalse();
        now = now.AddSeconds(1);
        for (var count = 1; count < 6; count++)
        {
            limits.TryReserve("coach@example.test", "confirmation").ShouldBeTrue();
            now = now.AddMinutes(1);
        }
        limits.TryReserve("coach@example.test", "confirmation").ShouldBeFalse();
        now = new(2027, 1, 1, 13, 0, 0, TimeSpan.Zero);
        limits.TryReserve("coach@example.test", "confirmation").ShouldBeTrue();
    }

    [Fact]
    public async Task SenderSuppressesRepeatedEmailsButPropagatesDeliveryFailureAsync()
    {
        var clock = Substitute.For<TimeProvider>();
        var now = DateTimeOffset.UtcNow;
        clock.GetUtcNow().Returns(_ => now);
        var delivery = Substitute.For<IMailDelivery>();
        var sender = new SmtpIdentityEmailSender(delivery, new(clock));
        var user = new ApplicationUser();
        await sender.SendConfirmationLinkAsync(user, "coach@example.test", "https://example.test/confirm?a=1&amp;b=2");
        await sender.SendConfirmationLinkAsync(user, "COACH@example.test", "https://example.test/another");
        await delivery.Received(1).SendAsync("coach@example.test", "Confirm your Pino account", Arg.Is<string>(value => value.Contains("?a=1&b=2", StringComparison.Ordinal)), CancellationToken.None);
        await delivery.DidNotReceive().SendAsync("COACH@example.test", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        delivery.SendAsync(Arg.Any<string>(), "Reset your Pino password", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromException(new IOException("SMTP unavailable")));
        await Should.ThrowAsync<IOException>(() => sender.SendPasswordResetLinkAsync(user, "coach@example.test", "https://example.test/reset"));
        await sender.SendPasswordResetCodeAsync(user, "coach@example.test", "123456");
        await delivery.DidNotReceive().SendAsync(Arg.Any<string>(), "Your Pino reset code", Arg.Any<string>(), Arg.Any<CancellationToken>());
        now = now.AddMinutes(1);
        await sender.SendPasswordResetCodeAsync(user, "coach@example.test", "123456");
        await delivery.Received(1).SendAsync("coach@example.test", "Your Pino reset code", Arg.Any<string>(), CancellationToken.None);
    }

    [Fact]
    public async Task AccountRequestLimitRejectsWithRetryHeaderAndDoesNotTrustForwardedIpAsync()
    {
        var options = new RateLimiterOptions();
        AccountRequestLimits.Configure(options);
        using var limiter = options.GlobalLimiter.ShouldNotBeNull();
        var http = new DefaultHttpContext();
        http.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.10");
        http.Request.Method = HttpMethods.Post;
        http.Request.Path = "/aCcOuNt/Login";
        for (var count = 0; count < 60; count++)
        {
            using var accepted = limiter.AttemptAcquire(http);
            accepted.IsAcquired.ShouldBeTrue();
        }
        http.Request.Headers["X-Forwarded-For"] = "192.0.2.20";
        using var rejected = limiter.AttemptAcquire(http);
        rejected.IsAcquired.ShouldBeFalse();
        await using var body = new MemoryStream();
        http.Response.Body = body;
        await options.OnRejected.ShouldNotBeNull()(new OnRejectedContext { HttpContext = http, Lease = rejected }, CancellationToken.None);
        http.Response.StatusCode.ShouldBe(429);
        int.Parse(http.Response.Headers.RetryAfter.ToString(), System.Globalization.CultureInfo.InvariantCulture).ShouldBeInRange(1, 60);
        http.Response.Headers.CacheControl.ToString().ShouldBe("no-store");
        http.Request.Method = HttpMethods.Get;
        using var read = limiter.AttemptAcquire(http);
        read.IsAcquired.ShouldBeTrue();
        http.Request.Method = HttpMethods.Post;
        http.Request.Path = "/api/clubs/example";
        using var sporting = limiter.AttemptAcquire(http);
        sporting.IsAcquired.ShouldBeTrue();
        http.Request.Path = "/Account/Login";
        http.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.20");
        using var differentAddress = limiter.AttemptAcquire(http);
        differentAddress.IsAcquired.ShouldBeTrue();
    }
}
