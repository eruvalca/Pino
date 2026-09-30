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
        await VerifyCoachCloseoutAsync(applicant, path, player.Id);
        (await owner.PostAsync<ClubReply>($"/api/clubs/{clubId}/members", new MemberChangeInput(pending.UserId, ClubRole.Coach, NewRole: null))).Kind.ShouldBe(ClubReplyKind.Saved);
        (await applicant.Context.APIRequest.GetAsync(path + $"/players/{player.Id}")).Status.ShouldBe(403);
        var token = await applicant.GetAsync<string>("/api/clubs/token");
        var deniedWrite = await applicant.Context.APIRequest.PostAsync(path + "/players", new() { Data = JsonSerializer.Serialize(player), Headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["X-Pino-CSRF"] = token, ["Content-Type"] = "application/json" } });
        deniedWrite.Status.ShouldBe(403, await deniedWrite.TextAsync());
        var bibPath = $"{path}/tryouts/{Guid.NewGuid()}/bib";
        var bibInput = new BibNumberInput(player.Id, "17", "");
        (await owner.Context.APIRequest.PostAsync(bibPath, new() { DataObject = bibInput })).Status.ShouldBe(400);
        var deniedBib = await applicant.Context.APIRequest.PostAsync(bibPath, new() { Data = JsonSerializer.Serialize(bibInput), Headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["X-Pino-CSRF"] = token, ["Content-Type"] = "application/json" } });
        deniedBib.Status.ShouldBe(403, await deniedBib.TextAsync());
        await VerifyDeniedCloseoutAsync(owner, applicant, path, token);
        await ResetPasswordAsync(applicant, email);
    }

    private static async Task VerifyCoachCloseoutAsync(BrowserSession coach, string path, Guid playerId)
    {
        var season = new SeasonInput { Name = "Coach season", StartsOn = new(2027, 1, 1), EndsOn = new(2027, 6, 30) };
        var tryout = new TryoutInput { SeasonId = season.Id, Name = "Coach tryout", Date = new(2027, 2, 1) };
        (await coach.PostAsync<SportReply>(path + "/seasons", season)).Kind.ShouldBe(SportReplyKind.Saved);
        (await coach.PostAsync<SportReply>(path + "/tryouts", tryout)).Kind.ShouldBe(SportReplyKind.Saved);
        var tryoutPath = $"{path}/tryouts/{tryout.Id}";
        (await coach.PostAsync<SportReply>(tryoutPath + "/players", new EnrollmentInput(playerId, "17"))).Kind.ShouldBe(SportReplyKind.Saved);
        (await coach.PostAsync<SportReply>(tryoutPath + "/decisions", new DecisionInput(Guid.NewGuid(), playerId, DecisionKind.Withdrawn, null, 1, 0, "Withdrawn"))).Kind.ShouldBe(SportReplyKind.Saved);
        var review = await coach.GetAsync<TryoutReview>(tryoutPath + "/review");
        var operation = Guid.NewGuid();
        (await coach.PostAsync<SportReply>(tryoutPath + "/close", new CloseTryoutInput(operation, review.ReviewToken))).Kind.ShouldBe(SportReplyKind.Saved);
        (await coach.GetAsync<TryoutReview>(tryoutPath + "/review")).Tryout.Closed.ShouldBeTrue();
        (await coach.PostAsync<SportReply>(tryoutPath + "/reopen", new ReopenTryoutInput(operation, "Coach correction"))).Kind.ShouldBe(SportReplyKind.Saved);
        (await coach.GetAsync<SeasonReview>($"{path}/seasons/{season.Id}/review")).Tryouts.ShouldHaveSingleItem().Closed.ShouldBeFalse();
    }

    private static async Task VerifyDeniedCloseoutAsync(BrowserSession owner, BrowserSession removed, string path, string token)
    {
        var tryoutPath = $"{path}/tryouts/{Guid.NewGuid()}";
        (await removed.Context.APIRequest.GetAsync(tryoutPath + "/review")).Status.ShouldBe(403);
        (await removed.Context.APIRequest.GetAsync($"{path}/seasons/{Guid.NewGuid()}/review")).Status.ShouldBe(403);
        foreach (var (endpoint, input) in new (string Endpoint, object Input)[]
        {
            ("close", new CloseTryoutInput(Guid.NewGuid(), "review")),
            ("reopen", new ReopenTryoutInput(Guid.NewGuid(), "Correction")),
        })
        {
            (await owner.Context.APIRequest.PostAsync(tryoutPath + "/" + endpoint, new() { DataObject = input })).Status.ShouldBe(400);
            var denied = await removed.Context.APIRequest.PostAsync(tryoutPath + "/" + endpoint, new() { Data = JsonSerializer.Serialize(input), Headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["X-Pino-CSRF"] = token, ["Content-Type"] = "application/json" } });
            denied.Status.ShouldBe(403, await denied.TextAsync());
        }
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
