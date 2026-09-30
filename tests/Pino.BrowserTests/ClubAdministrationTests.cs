using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Pino.Data;
using Pino.SharedKernel.Clubs;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed partial class ClubAdministrationTests
{
    private const string SkipReason = "Requires the local Aspire app, Mailpit and cleanup database connection.";
    private static CancellationToken Token => TestContext.Current.CancellationToken;
    private static ApplicationDbContext Database() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable("PINO_TEST_DATABASE"), options => options.EnableRetryOnFailure()).Options);

    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task InvitationsGrantOnlyVerifiedMatchingRolesAndMembershipMailTracksSavedOutcomesAsync()
    {
        await using var owner = await BrowserSession.CreateAsync();
        var ownerEmail = await owner.RegisterAsync();
        await owner.CompleteProfileAsync(throughUi: false);
        (await owner.PostAsync<ClubReply>("/api/clubs/create", new CreateClubInput { Name = "Test · Staff administration", Sport = "Soccer", City = "Chicago", State = "IL" })).Succeeded.ShouldBeTrue();
        var club = (await owner.GetAsync<AccessSnapshot>("/api/clubs/access")).Membership.ShouldNotBeNull().Club;
        var path = $"/api/clubs/{club.Id}";
        await using var invited = await BrowserSession.CreateAsync();
        var invitedEmail = invited.CreateEmail();
        var input = new InvitationInput { Email = invitedEmail, Role = ClubRole.Administrator };
        (await owner.Context.APIRequest.PostAsync(path + "/invitations", new() { DataObject = input })).Status.ShouldBe(400);
        (await owner.PostAsync<ClubReply>(path + "/invitations", input)).Succeeded.ShouldBeTrue();
        (await owner.PostAsync<ClubReply>(path + "/invitations", input)).Succeeded.ShouldBeTrue();
        (await owner.GetAsync<InvitationsPage>(path + "/invitations?page=0")).Items.ShouldHaveSingleItem();
        var link = await invited.EmailLinkAsync(invitedEmail, "Your Pino staff invitation");
        await invited.RegisterAsync(invitedEmail, new Uri(link).PathAndQuery);
        var token = QueryHelpers.ParseQuery(new Uri(link).Query)["token"].ToString();
        var previewPath = $"/api/clubs/invitations/{input.Id}?token={Uri.EscapeDataString(token)}";
        var acceptPath = $"/api/clubs/invitations/{input.Id}/accept";
        (await owner.GetAsync<InvitationPreview>(previewPath)).Club.ShouldBeNull();
        (await owner.PostAsync<ClubReply>(acceptPath, new InvitationAcceptInput(token))).Succeeded.ShouldBeFalse();
        var incomplete = await invited.GetAsync<InvitationPreview>(previewPath);
        incomplete.CanAccept.ShouldBeFalse();
        incomplete.Message.ShouldContain("profile photo");
        (await invited.PostAsync<ClubReply>(acceptPath, new InvitationAcceptInput(token))).Succeeded.ShouldBeFalse();
        await invited.CompleteProfileAsync(throughUi: false);
        await using var db = Database();
        var invitedUser = await db.Users.SingleAsync(value => value.Email == invitedEmail, Token);
        invitedUser.EmailConfirmed = false;
        await db.SaveChangesAsync(Token);
        (await invited.Context.APIRequest.GetAsync(previewPath)).Status.ShouldBe(403);
        invitedUser.EmailConfirmed = true;
        await db.SaveChangesAsync(Token);
        var invitation = await db.ClubInvitations.SingleAsync(value => value.Id == input.Id, Token);
        invitation.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-1);
        await db.SaveChangesAsync(Token);
        (await invited.GetAsync<InvitationPreview>(previewPath)).CanAccept.ShouldBeFalse();
        invitation.ExpiresAt = DateTimeOffset.UtcNow.AddDays(7);
        await db.SaveChangesAsync(Token);
        await invited.Page.GotoAsync(link);
        await invited.Page.Locator("main button.primary-action:enabled").WaitForAsync();
        await CaptureAsync(invited, "invitation-accept");
        await invited.Page.GetByRole(AriaRole.Button, new() { Name = "Accept invitation", Exact = true }).FocusAsync();
        await invited.Page.Keyboard.PressAsync("Enter");
        await invited.Page.GetByRole(AriaRole.Link, new() { Name = "Open your club", Exact = true }).WaitForAsync();
        (await invited.GetAsync<AccessSnapshot>("/api/clubs/access")).Membership.ShouldNotBeNull().Role.ShouldBe(ClubRole.Administrator);
        (await owner.GetAsync<InvitationsPage>(path + "/invitations?page=0")).Items.ShouldHaveSingleItem().DeliveryStatus.ShouldBe(StaffEmailStatus.Sent);
        (await invited.PostAsync<ClubReply>(acceptPath, new InvitationAcceptInput(token))).Succeeded.ShouldBeFalse();
        (await owner.PostAsync<ClubReply>(path + "/members", new MemberChangeInput(invitedUser.Id, ClubRole.Administrator, ClubRole.Coach))).Succeeded.ShouldBeTrue();
        await CheckAdministrativeBoundariesAsync(owner, invited, club);
        await owner.Page.GotoAsync($"/clubs/{club.Id}/invitations");
        await owner.Page.Locator("#invite-email:enabled").WaitForAsync();
        await CaptureAsync(owner, "staff-invitations");
        await owner.Page.GotoAsync($"/clubs/{club.Id}/settings");
        await owner.Page.Locator("#club-city:enabled").WaitForAsync();
        await owner.Page.GetByLabel("City", new() { Exact = true }).FillAsync("Evanston");
        await owner.Page.GetByRole(AriaRole.Button, new() { Name = "Save club details", Exact = true }).ClickAsync();
        await owner.Page.GetByText("Club details saved.", new() { Exact = true }).WaitForAsync();
        await owner.Page.Locator("#club-city:enabled").WaitForAsync();
        await CaptureAsync(owner, "club-details");
        var stale = new ClubDetailsInput { Revision = club.Revision, Name = club.Name, Sport = club.Sport, City = "Stale city", State = club.State };
        (await owner.PostAsync<ClubReply>(path + "/details", stale)).Kind.ShouldBe(ClubReplyKind.Conflict);
        (await owner.GetAsync<AccessSnapshot>("/api/clubs/access")).Membership.ShouldNotBeNull().Club.City.ShouldBe("Evanston");
        (await owner.PostAsync<ClubReply>(path + "/members", new MemberChangeInput(invitedUser.Id, ClubRole.Coach, null))).Succeeded.ShouldBeTrue();
        (await invited.PostAsync<ClubReply>(acceptPath, new InvitationAcceptInput(token))).Succeeded.ShouldBeFalse();
        (await invited.GetAsync<AccessSnapshot>("/api/clubs/access")).Membership.ShouldBeNull();
        await CheckResendAndRevocationAsync(owner, invited, db, club.Id, invitedEmail);
        (await invited.PostAsync<ClubReply>(path + "/requests", new { })).Succeeded.ShouldBeTrue();
        (await owner.EmailTextAsync(ownerEmail, "New Pino club join request")).ShouldContain("People & requests");
        var request = (await owner.GetAsync<PeoplePage>(path + "/people?requests=true&page=0")).People.ShouldHaveSingleItem();
        (await owner.PostAsync<ClubReply>(path + "/decide", new RequestDecisionInput(request.RequestId!.Value, false))).Succeeded.ShouldBeTrue();
        (await invited.EmailTextAsync(invitedEmail, "Your Pino club join request")).ShouldContain("denied");
        (await invited.PostAsync<ClubReply>(path + "/requests", new { })).Succeeded.ShouldBeTrue();
        request = (await owner.GetAsync<PeoplePage>(path + "/people?requests=true&page=0")).People.ShouldHaveSingleItem();
        (await owner.PostAsync<ClubReply>(path + "/decide", new RequestDecisionInput(request.RequestId!.Value, true))).Succeeded.ShouldBeTrue();
        (await invited.EmailTextAsync(invitedEmail, "Your Pino club join request", "approved")).ShouldContain("coach access");
        (await invited.GetAsync<AccessSnapshot>("/api/clubs/access")).Membership.ShouldNotBeNull().Role.ShouldBe(ClubRole.Coach);
        await owner.Page.GotoAsync($"/clubs/{club.Id}/emails");
        await owner.Page.Locator("main .sport-heading button:enabled").WaitForAsync();
        await CaptureAsync(owner, "staff-email-delivery");
        var deliveries = await owner.GetAsync<StaffEmailsPage>(path + "/emails?page=0");
        deliveries.Items.ShouldContain(value => value.Status == StaffEmailStatus.Sent && value.Subject == "Your Pino club join request");
        var stored = await db.StaffEmails.AsNoTracking().Where(value => value.ClubId == club.Id && value.Status == StaffEmailStatus.Sent).ToArrayAsync(Token);
        stored.ShouldAllBe(value => value.ProtectedBody == "" && value.SentAt != null);
        var export = await invited.Context.APIRequest.GetAsync("/api/clubs/personal-data");
        var json = await export.TextAsync();
        json.ShouldContain("Invitations");
        json.ShouldNotContain(token);
        json.ShouldNotContain("TokenHash");
        json.ShouldNotContain("ProtectedBody");
        await CheckJoinRequestLimitAsync(owner, invited, db, club.Id, invitedUser.Id);
    }

    private static async Task CheckJoinRequestLimitAsync(BrowserSession owner, BrowserSession applicant, ApplicationDbContext db, Guid clubId, string userId)
    {
        var path = $"/api/clubs/{clubId}";
        (await owner.PostAsync<ClubReply>(path + "/members", new MemberChangeInput(userId, ClubRole.Coach, null))).Succeeded.ShouldBeTrue();
        for (var count = 2; count < 5; count++)
        {
            (await applicant.PostAsync<ClubReply>(path + "/requests", new { })).Succeeded.ShouldBeTrue();
            var request = (await applicant.GetAsync<AccessSnapshot>("/api/clubs/access")).Request.ShouldNotBeNull();
            (await applicant.PostAsync<ClubReply>($"/api/clubs/requests/{request.Id}/cancel", new { })).Succeeded.ShouldBeTrue();
        }
        var before = await db.StaffEmails.CountAsync(value => value.ClubId == clubId, Token);
        (await applicant.PostAsync<ClubReply>(path + "/requests", new { })).Kind.ShouldBe(ClubReplyKind.Invalid);
        (await db.StaffEmails.CountAsync(value => value.ClubId == clubId, Token)).ShouldBe(before);
        (await db.ClubJoinRequests.CountAsync(value => value.UserId == userId, Token)).ShouldBe(5);
        await db.ClubJoinRequests.Where(value => value.UserId == userId).ExecuteUpdateAsync(update => update.SetProperty(value => value.CreatedAt, DateTimeOffset.UtcNow.AddHours(-2)), Token);
        (await applicant.PostAsync<ClubReply>(path + "/requests", new { })).Succeeded.ShouldBeTrue();
    }

    private static async Task CheckResendAndRevocationAsync(BrowserSession owner, BrowserSession invited, ApplicationDbContext db, Guid clubId, string email)
    {
        var path = $"/api/clubs/{clubId}";
        var input = new InvitationInput { Email = email, Role = ClubRole.Coach };
        (await owner.PostAsync<ClubReply>(path + "/invitations", input)).Succeeded.ShouldBeTrue();
        var body = await invited.EmailTextAsync(email, "Your Pino staff invitation", input.Id.ToString());
        var firstLink = body.Split('\n').Select(value => value.Trim()).Single(value => value.StartsWith("http", StringComparison.Ordinal));
        var original = QueryHelpers.ParseQuery(new Uri(firstLink).Query)["token"].ToString();
        (await owner.PostAsync<ClubReply>(path + "/invitations/resend", new InvitationChangeInput(input.Id, 1))).Kind.ShouldBe(ClubReplyKind.Invalid);
        await db.StaffEmails.Where(value => value.InvitationId == input.Id).ExecuteUpdateAsync(update => update.SetProperty(value => value.CreatedAt, DateTimeOffset.UtcNow.AddMinutes(-2)), Token);
        (await owner.PostAsync<ClubReply>(path + "/invitations/resend", new InvitationChangeInput(input.Id, 1))).Succeeded.ShouldBeTrue();
        (await invited.GetAsync<InvitationPreview>($"/api/clubs/invitations/{input.Id}?token={original}")).CanAccept.ShouldBeFalse();
        (await owner.PostAsync<ClubReply>(path + "/invitations/revoke", new InvitationChangeInput(input.Id, 1))).Kind.ShouldBe(ClubReplyKind.Conflict);
        (await owner.PostAsync<ClubReply>(path + "/invitations/revoke", new InvitationChangeInput(input.Id, 2))).Succeeded.ShouldBeTrue();
        (await owner.GetAsync<InvitationsPage>(path + "/invitations?page=0")).Items.Single(value => value.Id == input.Id).RevokedAt.ShouldNotBeNull();
    }

    private static async Task CheckAdministrativeBoundariesAsync(BrowserSession owner, BrowserSession coach, ClubSummary club)
    {
        var path = $"/api/clubs/{club.Id}";
        foreach (var endpoint in new[] { "/invitations?page=0", "/emails?page=0" })
        {
            (await coach.Context.APIRequest.GetAsync(path + endpoint)).Status.ShouldBe(403);
        }
        var token = await coach.GetAsync<string>("/api/clubs/token");
        foreach (var (endpoint, input) in new (string Endpoint, object Input)[]
        {
            ("/invitations", new InvitationInput { Email = "staff@example.test" }),
            ("/invitations/resend", new InvitationChangeInput(Guid.NewGuid(), 1)),
            ("/invitations/revoke", new InvitationChangeInput(Guid.NewGuid(), 1)),
            ("/emails/retry", new InvitationChangeInput(Guid.NewGuid(), 1)),
            ("/details", new ClubDetailsInput { Revision = club.Revision, Name = club.Name, Sport = club.Sport, City = club.City, State = club.State }),
        })
        {
            (await owner.Context.APIRequest.PostAsync(path + endpoint, new() { DataObject = input })).Status.ShouldBe(400);
            var response = await coach.Context.APIRequest.PostAsync(path + endpoint, new()
            {
                Data = JsonSerializer.Serialize(input),
                Headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["X-Pino-CSRF"] = token, ["Content-Type"] = "application/json" },
            });
            response.Status.ShouldBe(403, endpoint + ": " + await response.TextAsync());
        }
    }

    private static async Task CaptureAsync(BrowserSession session, string name)
    {
        await session.CaptureAsync(name + "-desktop");
        await session.Page.SetViewportSizeAsync(390, 844);
        await session.CaptureAsync(name + "-mobile");
        await session.Page.SetViewportSizeAsync(1440, 1000);
    }
}
