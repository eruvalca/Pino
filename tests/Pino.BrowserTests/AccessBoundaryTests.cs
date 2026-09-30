using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Playwright;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class AccessBoundaryTests
{
    [Fact(Skip = "Requires the local Aspire app, Mailpit and cleanup database connection.", SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task MembershipAndAntiforgeryProtectRecordsAndResetEmailWorksAsync()
    {
        await using var owner = await BrowserSession.CreateAsync();
        await owner.RegisterAsync();
        await owner.CompleteProfileAsync(throughUi: false);
        var club = await owner.PostAsync<ClubReply>("/api/clubs/create", new CreateClubInput { Name = "Test · Access boundaries", Sport = "Soccer", City = "Chicago", State = "IL" });
        club.Kind.ShouldBe(ClubReplyKind.Saved);
        var access = await owner.GetAsync<AccessSnapshot>("/api/clubs/access");
        var clubId = access.Membership.ShouldNotBeNull().Club.Id;
        var path = $"/api/clubs/{clubId}/sport";
        var player = new PlayerInput { FirstName = "Private", LastName = "Player", PlayerReference = "PRIVATE-1", GraduationYear = 2030 };
        (await owner.PostAsync<SportReply>(path + "/players", player)).Kind.ShouldBe(SportReplyKind.Saved);
        var withoutToken = await owner.Context.APIRequest.PostAsync(path + "/players", new() { DataObject = player });
        withoutToken.Status.ShouldBe(400);
        await using var applicant = await BrowserSession.CreateAsync();
        (await applicant.Context.APIRequest.GetAsync(path + "/overview")).Status.ShouldBe(401);
        var email = await applicant.RegisterAsync();
        await applicant.CompleteProfileAsync(throughUi: false);
        (await applicant.Context.APIRequest.GetAsync(path + $"/players/{player.Id}")).Status.ShouldBe(403);
        (await applicant.PostAsync<ClubReply>($"/api/clubs/{clubId}/requests", new { })).Kind.ShouldBe(ClubReplyKind.Saved);
        var requests = await owner.GetAsync<PeoplePage>($"/api/clubs/{clubId}/people?requests=true&page=0");
        var pending = requests.People.ShouldHaveSingleItem();
        (await owner.PostAsync<ClubReply>($"/api/clubs/{clubId}/decide", new RequestDecisionInput(pending.RequestId!.Value, Approve: true))).Kind.ShouldBe(ClubReplyKind.Saved);
        (await applicant.GetAsync<PlayerDetail>(path + $"/players/{player.Id}")).Player.FirstName.ShouldBe("Private");
        (await applicant.Context.APIRequest.GetAsync($"/api/clubs/{clubId}/people?requests=false&page=0")).Status.ShouldBe(403);
        (await owner.PostAsync<ClubReply>($"/api/clubs/{clubId}/members", new MemberChangeInput(pending.UserId, ClubRole.Coach, NewRole: null))).Kind.ShouldBe(ClubReplyKind.Saved);
        (await applicant.Context.APIRequest.GetAsync(path + $"/players/{player.Id}")).Status.ShouldBe(403);
        var token = await applicant.GetAsync<string>("/api/clubs/token");
        var deniedWrite = await applicant.Context.APIRequest.PostAsync(path + "/players", new() { Data = JsonSerializer.Serialize(player), Headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["X-Pino-CSRF"] = token, ["Content-Type"] = "application/json" } });
        deniedWrite.Status.ShouldBe(403, await deniedWrite.TextAsync());
        await ResetPasswordAsync(applicant, email);
    }

    private static async Task ResetPasswordAsync(BrowserSession session, string email)
    {
        var page = session.Page;
        await page.GotoAsync("/Account/ForgotPassword");
        await page.GetByLabel("Email", new() { Exact = true }).FillAsync(email);
        await page.GetByRole(AriaRole.Button, new() { Name = "Reset password", Exact = true }).ClickAsync();
        await page.WaitForURLAsync("**/Account/ForgotPasswordConfirmation");
        await page.GotoAsync(await session.EmailLinkAsync(email, "Reset your Pino password"));
        await page.GetByLabel("Email", new() { Exact = true }).FillAsync(email);
        const string NewPassword = "Changed-for-test!47";
        await page.GetByLabel("Password", new() { Exact = true }).FillAsync(NewPassword);
        await page.GetByLabel("Confirm password", new() { Exact = true }).FillAsync(NewPassword);
        await page.GetByRole(AriaRole.Button, new() { Name = "Reset", Exact = true }).ClickAsync();
        await page.WaitForURLAsync("**/Account/ResetPasswordConfirmation");
        await session.LoginAsync(email, NewPassword);
        (await session.GetAsync<AccessSnapshot>("/api/clubs/access")).Profile.FirstName.ShouldBe("Avery");
    }
}
