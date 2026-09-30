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
        var ownerPhoto = access.Profile.PhotoUrl.ShouldNotBeNull();
        (await applicant.Context.APIRequest.GetAsync(ownerPhoto.ToString())).Status.ShouldBe(401);
        var email = await applicant.RegisterAsync();
        await applicant.CompleteProfileAsync(throughUi: false);
        (await applicant.Context.APIRequest.GetAsync(ownerPhoto.ToString())).Status.ShouldBe(404);
        (await applicant.Context.APIRequest.GetAsync(path + $"/players/{player.Id}")).Status.ShouldBe(403);
        (await applicant.Context.APIRequest.GetAsync(path + $"/players/{player.Id}/personal-data")).Status.ShouldBe(403);
        (await applicant.PostAsync<ClubReply>($"/api/clubs/{clubId}/requests", new { })).Kind.ShouldBe(ClubReplyKind.Saved);
        var requests = await owner.GetAsync<PeoplePage>($"/api/clubs/{clubId}/people?requests=true&page=0");
        var pending = requests.People.ShouldHaveSingleItem();
        (await owner.PostAsync<ClubReply>($"/api/clubs/{clubId}/decide", new RequestDecisionInput(pending.RequestId!.Value, Approve: true))).Kind.ShouldBe(ClubReplyKind.Saved);
        (await applicant.GetAsync<PlayerDetail>(path + $"/players/{player.Id}")).Player.FirstName.ShouldBe("Private");
        (await applicant.Context.APIRequest.GetAsync(path + $"/players/{player.Id}/personal-data")).Status.ShouldBe(403);
        (await applicant.Context.APIRequest.GetAsync($"/api/clubs/{clubId}/people?requests=false&page=0")).Status.ShouldBe(403);
        await VerifyCoachCloseoutAsync(applicant, path, player.Id);
        var coachPhoto = new Uri($"/api/clubs/photos/{Uri.EscapeDataString(pending.UserId)}", UriKind.Relative);
        (await owner.Context.APIRequest.GetAsync(coachPhoto.ToString())).Status.ShouldBe(200);
        (await applicant.Context.APIRequest.GetAsync(ownerPhoto.ToString())).Status.ShouldBe(200);
        var coachToken = await applicant.GetAsync<string>("/api/clubs/token");
        await VerifyAdministrativePreparationAsync(owner, applicant, path, coachToken);
        var erasure = new ErasePlayerInput(Guid.NewGuid(), player.Id, 1, "Private Player", true);
        (await owner.Context.APIRequest.PostAsync(path + "/players/erase", new() { DataObject = erasure })).Status.ShouldBe(400);
        var deniedErasure = await applicant.Context.APIRequest.PostAsync(path + "/players/erase", new() { Data = JsonSerializer.Serialize(erasure), Headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["X-Pino-CSRF"] = coachToken, ["Content-Type"] = "application/json" } });
        deniedErasure.Status.ShouldBe(403);
        var redactPath = $"{path}/tryouts/{Guid.NewGuid()}/notes/redact";
        var redact = new RedactNoteInput(Guid.NewGuid(), Guid.NewGuid(), "Sensitive content", true);
        (await owner.Context.APIRequest.PostAsync(redactPath, new() { DataObject = redact })).Status.ShouldBe(400);
        (await applicant.Context.APIRequest.PostAsync(redactPath, new() { Data = JsonSerializer.Serialize(redact), Headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["X-Pino-CSRF"] = coachToken, ["Content-Type"] = "application/json" } })).Status.ShouldBe(403);
        (await owner.PostAsync<ClubReply>($"/api/clubs/{clubId}/members", new MemberChangeInput(pending.UserId, ClubRole.Coach, NewRole: null))).Kind.ShouldBe(ClubReplyKind.Saved);
        await VerifyDepartedStaffHistoryAsync(owner, clubId, player.Id, coachPhoto);
        (await applicant.Context.APIRequest.GetAsync(ownerPhoto.ToString())).Status.ShouldBe(404);
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
        await applicant.Page.GotoAsync("/Account/Manage/DeletePersonalData");
        await applicant.SubmitAccountFormAsync("/Account/Manage/DeletePersonalData", "Delete data and close my account",
            () => applicant.Page.GetByLabel("Password", new() { Exact = true }).FillAsync("Changed-for-test!47"));
        await applicant.Page.WaitForURLAsync("**/Account/Login?**");
        await VerifyDepartedStaffHistoryAsync(owner, clubId, player.Id, coachPhoto);
    }

    private static async Task VerifyDepartedStaffHistoryAsync(BrowserSession owner, Guid clubId, Guid playerId, Uri photo)
    {
        var path = $"/api/clubs/{clubId}/sport";
        var tryout = (await owner.GetAsync<SportOverview>(path + "/overview")).Tryouts.ShouldHaveSingleItem();
        var notebook = await owner.GetAsync<PlayerNotebook>($"{path}/tryouts/{tryout.Id}/players/{playerId}/notebook");
        notebook.Notes.ShouldHaveSingleItem().Author.ShouldBe("Avery Coach");
        notebook.Notes[0].AuthorPhotoUrl.ShouldBe(photo);
        notebook.History.ShouldNotBeEmpty();
        notebook.History.ShouldAllBe(value => value.AuthorPhotoUrl == photo && value.Author == "Avery Coach");
        (await owner.Context.APIRequest.GetAsync(photo.ToString())).Status.ShouldBe(404);
        await owner.Page.GotoAsync($"/clubs/{clubId}/players/{playerId}");
        await owner.Page.Locator("#history-tryout:enabled").WaitForAsync();
        await owner.Page.GetByLabel("Tryout to review", new() { Exact = true }).SelectOptionAsync(tryout.Id.ToString());
        await owner.Page.GetByText("Coaches can add notes", new() { Exact = true }).WaitForAsync();
        await owner.Page.WaitForFunctionAsync("() => [...document.querySelectorAll('.player-tryout-history .staff-avatar')].some(avatar => !avatar.querySelector('img') && avatar.textContent === 'AC')");
    }

    private static async Task VerifyAdministrativePreparationAsync(BrowserSession owner, BrowserSession coach, string path, string token)
    {
        var tryoutId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        foreach (var endpoint in new[] { $"/tryouts/{tryoutId}/enrollment-candidates", $"/tryouts/{tryoutId}/returning?sourceSeasonId={seasonId}" })
        {
            (await coach.Context.APIRequest.GetAsync(path + endpoint)).Status.ShouldBe(403);
        }
        foreach (var (endpoint, input) in new (string Endpoint, object Input)[]
        {
            ($"/tryouts/{tryoutId}/bulk-enrollment", new BulkEnrollmentInput(Guid.NewGuid(), [])),
            ($"/tryouts/{tryoutId}/returning", new ReturningPlacementInput(Guid.NewGuid(), seasonId, [])),
            ($"/seasons/{seasonId}/team-availability", new TeamAvailabilityInput(Guid.NewGuid(), true, 0)),
            ($"/teams/{Guid.NewGuid()}/targets", new TeamTargetsInput(1, null, [])),
        })
        {
            (await owner.Context.APIRequest.PostAsync(path + endpoint, new() { DataObject = input })).Status.ShouldBe(400);
            (await coach.Context.APIRequest.PostAsync(path + endpoint, new() { Data = JsonSerializer.Serialize(input), Headers = new Dictionary<string, string>(StringComparer.Ordinal) { ["X-Pino-CSRF"] = token, ["Content-Type"] = "application/json" } })).Status.ShouldBe(403);
        }
    }

    private static async Task VerifyCoachCloseoutAsync(BrowserSession coach, string path, Guid playerId)
    {
        var season = new SeasonInput { Name = "Coach season", StartsOn = new(2027, 1, 1), EndsOn = new(2027, 6, 30) };
        var tryout = new TryoutInput { SeasonId = season.Id, Name = "Coach tryout", Date = new(2027, 2, 1) };
        (await coach.PostAsync<SportReply>(path + "/seasons", season)).Kind.ShouldBe(SportReplyKind.Saved);
        (await coach.PostAsync<SportReply>(path + "/tryouts", tryout)).Kind.ShouldBe(SportReplyKind.Saved);
        var tryoutPath = $"{path}/tryouts/{tryout.Id}";
        (await coach.PostAsync<SportReply>(tryoutPath + "/bib", new BibNumberInput(playerId, "17", ""))).Kind.ShouldBe(SportReplyKind.Saved);
        (await coach.PostAsync<SportReply>(tryoutPath + "/attendance", new AttendanceInput(playerId, AttendanceKind.Absent, 0))).Kind.ShouldBe(SportReplyKind.Saved);
        (await coach.PostAsync<SportReply>(tryoutPath + "/notes", new NoteInput(Guid.NewGuid(), playerId, "Coaches can add notes"))).Kind.ShouldBe(SportReplyKind.Saved);
        (await coach.PostAsync<SportReply>(tryoutPath + "/decisions", new DecisionInput(Guid.NewGuid(), playerId, DecisionKind.Withdrawn, null, 1, 0, "Withdrawn"))).Kind.ShouldBe(SportReplyKind.Saved);
        var review = await coach.GetAsync<TryoutReview>(tryoutPath + "/review");
        var operation = Guid.NewGuid();
        (await coach.PostAsync<SportReply>(tryoutPath + "/close", new CloseTryoutInput(operation, review.ReviewToken))).Kind.ShouldBe(SportReplyKind.Saved);
        (await coach.GetAsync<TryoutReview>(tryoutPath + "/review")).Tryout.Closed.ShouldBeTrue();
        (await coach.Context.APIRequest.GetAsync(tryoutPath + "/results.csv")).Status.ShouldBe(200);
        (await coach.Context.APIRequest.GetAsync(tryoutPath + $"/results.csv?editionId={operation}")).Status.ShouldBe(200);
        (await coach.Context.APIRequest.GetAsync($"{path}/seasons/{season.Id}/roster.csv")).Status.ShouldBe(200);
        (await coach.PostAsync<SportReply>(tryoutPath + "/reopen", new ReopenTryoutInput(operation, "Coach correction"))).Kind.ShouldBe(SportReplyKind.Saved);
        (await coach.GetAsync<SeasonReview>($"{path}/seasons/{season.Id}/review")).Tryouts.ShouldHaveSingleItem().Closed.ShouldBeFalse();
        var entry = (await coach.GetAsync<TryoutDetail>(tryoutPath)).Roster.ShouldHaveSingleItem();
        (await coach.PostAsync<SportReply>(tryoutPath + "/enrollments", new EnrollmentChangeInput(Guid.NewGuid(), playerId, true, "Coach corrected an enrollment mistake", entry.Revision, entry.PlacementRevision))).Kind.ShouldBe(SportReplyKind.Saved);
        var removed = (await coach.GetAsync<EnrollmentDetail[]>(tryoutPath + "/enrollments")).ShouldHaveSingleItem();
        removed.Removed.ShouldBeTrue();
        (await coach.PostAsync<SportReply>(tryoutPath + "/enrollments", new EnrollmentChangeInput(Guid.NewGuid(), playerId, false, "Coach restored enrollment", removed.Entry.Revision, removed.Entry.PlacementRevision, "17"))).Kind.ShouldBe(SportReplyKind.Saved);
        var restored = (await coach.GetAsync<TryoutDetail>(tryoutPath)).Roster.ShouldHaveSingleItem();
        var excluded = await coach.PostAsync<SportingBatchReport>(tryoutPath + "/enrollments/batch", new BulkEnrollmentChangeInput(Guid.NewGuid(), true, "Coach reviewed group exclusion", [new(playerId, restored.Revision, restored.PlacementRevision)]));
        excluded.Applied.ShouldBe(1);
        (await coach.GetAsync<TryoutDetail>(tryoutPath)).Roster.ShouldBeEmpty();
    }

    private static async Task VerifyDeniedCloseoutAsync(BrowserSession owner, BrowserSession removed, string path, string token)
    {
        var tryoutPath = $"{path}/tryouts/{Guid.NewGuid()}";
        (await removed.Context.APIRequest.GetAsync(tryoutPath + "/review")).Status.ShouldBe(403);
        (await removed.Context.APIRequest.GetAsync(tryoutPath + "/enrollments")).Status.ShouldBe(403);
        var deniedNotebook = await removed.Context.APIRequest.GetAsync($"{tryoutPath}/players/{Guid.NewGuid()}/notebook");
        deniedNotebook.Status.ShouldBe(403, await deniedNotebook.TextAsync());
        (await removed.Context.APIRequest.GetAsync($"{path}/seasons/{Guid.NewGuid()}/review")).Status.ShouldBe(403);
        foreach (var endpoint in new[] { $"/seasons/{Guid.NewGuid()}/roster.csv", $"/teams/{Guid.NewGuid()}/roster.csv?seasonId={Guid.NewGuid()}", $"/tryouts/{Guid.NewGuid()}/results.csv", $"/players/{Guid.NewGuid()}/personal-data" })
        {
            (await removed.Context.APIRequest.GetAsync(path + endpoint)).Status.ShouldBe(403);
        }
        foreach (var (endpoint, input) in new (string Endpoint, object Input)[]
        {
            ("close", new CloseTryoutInput(Guid.NewGuid(), "review")),
            ("reopen", new ReopenTryoutInput(Guid.NewGuid(), "Correction")),
            ("enrollments", new EnrollmentChangeInput(Guid.NewGuid(), Guid.NewGuid(), true, "Correction", 1, 0)),
            ("enrollments/batch", new BulkEnrollmentChangeInput(Guid.NewGuid(), true, "Group correction", [new(Guid.NewGuid(), 1, 0)])),
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
        await session.SubmitAccountFormAsync("/Account/ForgotPassword", "Reset password", () => page.GetByLabel("Email", new() { Exact = true }).FillAsync(email));
        await page.WaitForURLAsync("**/Account/ForgotPasswordConfirmation");
        await page.GotoAsync(await session.EmailLinkAsync(email, "Reset your Pino password"));
        const string NewPassword = "Changed-for-test!47";
        await session.SubmitAccountFormAsync("/Account/ResetPassword", "Reset", async () =>
        {
            await page.GetByLabel("Email", new() { Exact = true }).FillAsync(email);
            await page.GetByLabel("Password", new() { Exact = true }).FillAsync(NewPassword);
            await page.GetByLabel("Confirm password", new() { Exact = true }).FillAsync(NewPassword);
        });
        await page.WaitForURLAsync("**/Account/ResetPasswordConfirmation");
        await session.LoginAsync(email, NewPassword);
        (await session.GetAsync<AccessSnapshot>("/api/clubs/access")).Profile.FirstName.ShouldBe("Avery");
    }
}
