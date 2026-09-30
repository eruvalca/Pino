using Pino.SharedKernel.Sporting;
using Shouldly;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    private static async Task VerifySeasonIndependenceAsync(BrowserSession session, string path, SportOverview overview)
    {
        var player = (await session.GetAsync<PlayerPage>(path + "/players?query=Jordan&archived=false&page=0")).Players.ShouldHaveSingleItem();
        var original = overview.Tryouts.Single();
        var firstTeam = overview.Teams.Single();
        await PlaceAsync(session, path, original.Id, player.Id, firstTeam.Id);
        var later = new TryoutInput { SeasonId = original.SeasonId, Name = "Follow-up evaluation", Date = original.Date.AddDays(7) };
        (await session.PostAsync<SportReply>(path + "/tryouts", later)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>($"{path}/tryouts/{later.Id}/players", new EnrollmentInput(player.Id, "17"))).Kind.ShouldBe(SportReplyKind.Saved);
        await PlaceAsync(session, path, later.Id, player.Id, firstTeam.Id);
        var originalState = await session.GetAsync<TryoutDetail>($"{path}/tryouts/{original.Id}");
        var entry = originalState.Roster.Single(value => value.Player.Id == player.Id);
        var withdraw = new DecisionInput(Guid.NewGuid(), player.Id, DecisionKind.Withdrawn, TeamId: null, entry.Revision, entry.PlacementRevision, "Withdrawn from first evaluation only.");
        (await session.PostAsync<SportReply>($"{path}/tryouts/{original.Id}/decisions", withdraw)).Kind.ShouldBe(SportReplyKind.Saved);
        var preserved = (await session.GetAsync<PlayerDetail>($"{path}/players/{player.Id}")).Placements.ShouldHaveSingleItem();
        preserved.TeamId.ShouldBe(firstTeam.Id);
        preserved.Tryout.ShouldBe("Follow-up evaluation");

        var nextSeason = new SeasonInput { Name = "Spring 2028", StartsOn = new(2028, 1, 1), EndsOn = new(2028, 6, 30) };
        (await session.PostAsync<SportReply>(path + "/seasons", nextSeason)).Kind.ShouldBe(SportReplyKind.Saved);
        var nextTeam = new TeamInput { SeasonId = nextSeason.Id, Name = "Northside Silver", GraduationYear = 2030 };
        (await session.PostAsync<SportReply>(path + "/teams", nextTeam)).Kind.ShouldBe(SportReplyKind.Saved);
        var nextTryout = new TryoutInput { SeasonId = nextSeason.Id, Name = "Spring 2028 evaluation", Date = new(2028, 2, 1) };
        (await session.PostAsync<SportReply>(path + "/tryouts", nextTryout)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>($"{path}/tryouts/{nextTryout.Id}/players", new EnrollmentInput(player.Id, ""))).Kind.ShouldBe(SportReplyKind.Saved);
        await PlaceAsync(session, path, nextTryout.Id, player.Id, nextTeam.Id);
        var result = await session.GetAsync<PlayerDetail>($"{path}/players/{player.Id}");
        result.Placements.Count.ShouldBe(2);
        result.Placements.Single(value => value.SeasonId == original.SeasonId).TeamId.ShouldBe(firstTeam.Id);
        result.Placements.Single(value => value.SeasonId == nextSeason.Id).TeamId.ShouldBe(nextTeam.Id);
        result.History.ShouldContain(value => value.Team == "Northside Blue" && value.Season == "Spring 2027");
        nextTeam.Revision = 1; nextTeam.Archived = true;
        (await session.PostAsync<SportReply>(path + "/teams", nextTeam)).Kind.ShouldBe(SportReplyKind.Invalid);
        nextTeam.Archived = false; nextTeam.Name = "Northside Silver renamed";
        (await session.PostAsync<SportReply>(path + "/teams", nextTeam)).Kind.ShouldBe(SportReplyKind.Saved);
        var renamed = await session.GetAsync<PlayerDetail>($"{path}/players/{player.Id}");
        renamed.Placements.Single(value => value.SeasonId == nextSeason.Id).Team.ShouldBe("Northside Silver renamed");
        renamed.History.ShouldContain(value => value.Team == "Northside Silver");
    }

    private static async Task PlaceAsync(BrowserSession session, string path, Guid tryoutId, Guid playerId, Guid teamId)
    {
        var detail = await session.GetAsync<TryoutDetail>($"{path}/tryouts/{tryoutId}");
        var entry = detail.Roster.Single(value => value.Player.Id == playerId);
        var input = new DecisionInput(Guid.NewGuid(), playerId, DecisionKind.Placed, teamId, entry.Revision, entry.PlacementRevision, "Season placement.");
        (await session.PostAsync<SportReply>($"{path}/tryouts/{tryoutId}/decisions", input)).Kind.ShouldBe(SportReplyKind.Saved);
    }
}
