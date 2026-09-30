using Pino.SharedKernel.Sporting;
using Shouldly;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    private static async Task VerifyCloseoutLocksAsync(BrowserSession session, CloseoutFixture data, Guid closeoutId)
    {
        var path = $"/api/clubs/{data.ClubId}/sport";
        var tryoutPath = $"{path}/tryouts/{data.Tryout.Id}";
        var detail = await session.GetAsync<TryoutDetail>(tryoutPath);
        var entry = detail.Roster.Single(value => value.Player.Id == data.Player.Id);
        (await session.PostAsync<SportReply>(tryoutPath + "/players", new EnrollmentInput(data.Player.Id, ""))).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/bib", new BibNumberInput(data.Player.Id, "99", "41"))).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", new NoteInput(Guid.NewGuid(), data.Player.Id, "Must be rejected"))).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", new NoteInput(Guid.NewGuid(), data.Player.Id, "Rejected correction", data.Note.Id))).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", data.Note)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/decisions", new DecisionInput(Guid.NewGuid(), data.Player.Id, DecisionKind.Withdrawn, null, entry.Revision, entry.PlacementRevision, "Rejected"))).Kind.ShouldBe(SportReplyKind.Invalid);
        data.Tryout.Revision = detail.Tryout.Revision;
        (await session.PostAsync<SportReply>(path + "/tryouts", data.Tryout)).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/close", new CloseTryoutInput(closeoutId, "retry-of-committed-operation"))).Kind.ShouldBe(SportReplyKind.Saved);
        var saved = await session.GetAsync<TryoutDetail>(tryoutPath);
        saved.Notes.ShouldHaveSingleItem().Text.ShouldBe(data.Note.Text);
        saved.Roster.Single(value => value.Player.Id == data.Player.Id).Bib.ShouldBe("41");
        saved.Roster.Single(value => value.Player.Id == data.Player.Id).Decision.ShouldBe(DecisionKind.Placed);
        data.Season.Revision = 1;
        data.Season.Archived = true;
        (await session.PostAsync<SportReply>(path + "/seasons", data.Season)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/reopen", new ReopenTryoutInput(closeoutId, "Correction"))).Kind.ShouldBe(SportReplyKind.Invalid);
        data.Season.Revision++;
        data.Season.Archived = false;
        (await session.PostAsync<SportReply>(path + "/seasons", data.Season)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.GetAsync<TryoutReview>(tryoutPath + "/review")).Tryout.Closed.ShouldBeTrue();
    }

    private static async Task VerifyCloseoutHistoryAsync(BrowserSession session, CloseoutFixture data, TryoutCloseoutSummary edition)
    {
        var path = $"/api/clubs/{data.ClubId}/sport";
        data.Player.Revision = 1;
        data.Player.FirstName = "Jordan Updated";
        (await session.PostAsync<SportReply>(path + "/players", data.Player)).Kind.ShouldBe(SportReplyKind.Saved);
        data.Blue.Revision = 1;
        data.Blue.Name = "Northside Blue Updated";
        (await session.PostAsync<SportReply>(path + "/teams", data.Blue)).Kind.ShouldBe(SportReplyKind.Saved);
        var later = new TryoutInput { SeasonId = data.Season.Id, Name = "Follow-up evaluations", Date = new(2027, 3, 1) };
        (await session.PostAsync<SportReply>(path + "/tryouts", later)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>($"{path}/tryouts/{later.Id}/players", new EnrollmentInput(data.Player.Id, "17"))).Kind.ShouldBe(SportReplyKind.Saved);
        await PlaceAsync(session, path, later.Id, data.Player.Id, data.Silver.Id);
        var retained = (await session.GetAsync<TryoutReview>($"{path}/tryouts/{data.Tryout.Id}/review")).Closeouts.ShouldHaveSingleItem();
        retained.Id.ShouldBe(edition.Id);
        retained.Results.Single(value => value.PlayerId == data.Player.Id).FirstName.ShouldBe("Jordan");
        retained.Results.Single(value => value.PlayerId == data.Player.Id).TeamName.ShouldBe("Northside Blue");
        var season = await session.GetAsync<SeasonReview>($"{path}/seasons/{data.Season.Id}/review");
        season.Players.ShouldBe(3);
        season.Teams.Single(value => value.Team.Id == data.Blue.Id).Players.ShouldBeEmpty();
        season.Teams.Single(value => value.Team.Id == data.Silver.Id).Players.ShouldHaveSingleItem().FirstName.ShouldBe("Jordan Updated");
        season.UnplacedPlayers.Count.ShouldBe(2);
        await session.Page.GotoAsync($"/clubs/{data.ClubId}/seasons/{data.Season.Id}/review");
        await session.Page.Locator(".sport-heading button:enabled").WaitForAsync();
        await session.Page.GetByRole(Microsoft.Playwright.AriaRole.Link, new() { Name = "Current team rosters", Exact = true }).ClickAsync();
        session.Page.Url.ShouldEndWith($"/seasons/{data.Season.Id}/review#current-teams");
        await CaptureReviewAsync(session, "season-review");
    }
}
