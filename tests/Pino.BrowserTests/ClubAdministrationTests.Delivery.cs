using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Pino.Data;
using Pino.Features.Clubs.Data;
using Pino.Features.Clubs.Services;
using Pino.Services.Mail;
using Pino.SharedKernel.Clubs;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

public sealed partial class ClubAdministrationTests
{
    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task FailedStaffMailRecoversWithLeasesAndNeverSendsExpiredInvitationsAsync()
    {
        await using var owner = await BrowserSession.CreateAsync();
        var address = await owner.RegisterAsync();
        await owner.CompleteProfileAsync(throughUi: false);
        (await owner.PostAsync<ClubReply>("/api/clubs/create", new CreateClubInput { Name = "Test · Mail recovery", Sport = "Soccer", City = "Chicago", State = "IL" })).Succeeded.ShouldBeTrue();
        var club = (await owner.GetAsync<AccessSnapshot>("/api/clubs/access")).Membership.ShouldNotBeNull().Club;
        var clock = new DeliveryClock();
        var factory = new DeliveryContextFactory();
        await using var db = factory.CreateDbContext();
        var user = await db.Users.SingleAsync(value => value.Email == address, Token);
        var actor = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.Id)], "BrowserTest"));
        var mail = new ClubMail(new EphemeralDataProtectionProvider(), new ConfigurationBuilder().Build(), Substitute.For<IWebHostEnvironment>(), clock);
        var delivery = Substitute.For<IMailDelivery>();
        var failing = true;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        delivery.SendAsync(address, "Synthetic delivery", "Saved membership notification", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (failing) { return Task.FromException(new IOException("Synthetic mail outage")); }
            entered.TrySetResult();
            return complete.Task;
        });
        using var worker = new StaffEmailDelivery(factory, delivery, mail, clock, NullLogger<StaffEmailDelivery>.Instance);
        var queued = mail.Compose(club.Id, address, user.Id, "Synthetic delivery", "Saved membership notification");
        db.StaffEmails.Add(queued);
        await db.SaveChangesAsync(Token);
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            (await worker.DeliverAsync(queued.Id, Token)).ShouldBeTrue();
            var current = await db.StaffEmails.AsNoTracking().SingleAsync(value => value.Id == queued.Id, Token);
            current.Attempts.ShouldBe(attempt);
            current.Status.ShouldBe(attempt == 5 ? StaffEmailStatus.Failed : StaffEmailStatus.Pending);
            current.ProtectedBody.ShouldNotBeEmpty();
            current.SentAt.ShouldBeNull();
            current.NextAttemptAt.ShouldBeGreaterThan(clock.GetUtcNow());
            clock.Now = clock.Now.AddMinutes(10);
        }
        var failed = await db.StaffEmails.AsNoTracking().SingleAsync(value => value.Id == queued.Id, Token);
        var service = new ClubService(factory, Substitute.For<IProfilePhotoStore>(), clock, mail);
        (await service.RetryEmailAsync(actor, club.Id, new(queued.Id, failed.Revision - 1), Token)).ToReply().Kind.ShouldBe(ClubReplyKind.Conflict);
        (await service.RetryEmailAsync(actor, club.Id, new(queued.Id, failed.Revision), Token)).ToReply().Succeeded.ShouldBeTrue();
        failing = false;
        var first = worker.DeliverAsync(queued.Id, Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
        (await worker.DeliverAsync(queued.Id, Token)).ShouldBeFalse();
        complete.SetResult();
        (await first).ShouldBeTrue();
        var sent = await db.StaffEmails.AsNoTracking().SingleAsync(value => value.Id == queued.Id, Token);
        sent.Status.ShouldBe(StaffEmailStatus.Sent);
        sent.Attempts.ShouldBe(1);
        sent.SentAt.ShouldBe(clock.GetUtcNow());
        sent.ProtectedBody.ShouldBeEmpty();
        sent.LeaseId.ShouldBeNull();
        await delivery.Received(6).SendAsync(address, "Synthetic delivery", "Saved membership notification", Arg.Any<CancellationToken>());
        (await owner.GetAsync<AccessSnapshot>("/api/clubs/access")).Membership.ShouldNotBeNull().Club.Id.ShouldBe(club.Id);

        var abandoned = mail.Compose(club.Id, address, user.Id, "Synthetic delivery", "Saved membership notification");
        abandoned.Status = StaffEmailStatus.Sending;
        abandoned.LeaseId = Guid.NewGuid();
        abandoned.LeaseUntil = clock.Now.AddSeconds(-1);
        db.StaffEmails.Add(abandoned);
        await db.SaveChangesAsync(Token);
        (await worker.DeliverAsync(abandoned.Id, Token)).ShouldBeTrue();
        (await db.StaffEmails.AsNoTracking().SingleAsync(value => value.Id == abandoned.Id, Token)).Status.ShouldBe(StaffEmailStatus.Sent);

        var invitation = new ClubInvitation
        {
            Id = Guid.NewGuid(),
            ClubId = club.Id,
            Email = address,
            NormalizedEmail = address.ToUpperInvariant(),
            CreatedById = user.Id,
            CreatedAt = clock.Now.AddDays(-7),
            ExpiresAt = clock.Now,
            TokenHash = InvitationTokens.Hash(new string('x', 43)),
        };
        db.ClubInvitations.Add(invitation);
        var obsolete = mail.Compose(club.Id, address, null, "Expired invitation", "Must not send");
        obsolete.InvitationId = invitation.Id;
        obsolete.InvitationRevision = invitation.Revision;
        db.StaffEmails.Add(obsolete);
        await db.SaveChangesAsync(Token);
        (await worker.DeliverAsync(obsolete.Id, Token)).ShouldBeTrue();
        var cancelled = await db.StaffEmails.AsNoTracking().SingleAsync(value => value.Id == obsolete.Id, Token);
        cancelled.Status.ShouldBe(StaffEmailStatus.Cancelled);
        cancelled.ProtectedBody.ShouldBeEmpty();
        await delivery.DidNotReceive().SendAsync(address, "Expired invitation", Arg.Any<string>(), Arg.Any<CancellationToken>());
        var preview = await service.PreviewInvitationAsync(actor, invitation.Id, new string('x', 43), Token);
        preview.CanAccept.ShouldBeFalse();
        preview.Club.ShouldBeNull();
    }

    private sealed class DeliveryClock : TimeProvider
    {
        // Future-dated rows keep this isolated fault-injection fixture out of the live worker's queue.
        internal DateTimeOffset Now { get; set; } = new(6000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private sealed class DeliveryContextFactory : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => Database();
    }
}
