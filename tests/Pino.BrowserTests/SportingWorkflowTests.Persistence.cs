using Pino.SharedKernel.Sporting;
using Shouldly;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    private static async Task VerifyConflictAndHistoryAsync(BrowserSession session, string path, TryoutDetail detail, SportOverview overview)
    {
        var entry = detail.Roster.Single();
        var tryoutPath = $"{path}/tryouts/{detail.Tryout.Id}";
        var placement = detail.History.Single();
        var changedTeam = new DecisionInput(placement.Id, entry.Player.Id, DecisionKind.Placed, Guid.NewGuid(), entry.Revision, entry.PlacementRevision, placement.Reason);
        (await session.PostAsync<SportReply>(tryoutPath + "/decisions", changedTeam)).Kind.ShouldBe(SportReplyKind.Conflict);
        var note = detail.Notes.Single();
        var correction = new NoteInput(Guid.NewGuid(), entry.Player.Id, "Correction: finds the near-side runner.", note.Id);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", correction)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", correction)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", correction with { Text = "Different text under the same ID." })).Kind.ShouldBe(SportReplyKind.Conflict);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", correction with { CorrectsId = null })).Kind.ShouldBe(SportReplyKind.Conflict);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", correction with { Id = Guid.NewGuid() })).Kind.ShouldBe(SportReplyKind.Conflict);
        var withdrawal = new DecisionInput(Guid.NewGuid(), entry.Player.Id, DecisionKind.Withdrawn, TeamId: null, entry.Revision, entry.PlacementRevision, "Family withdrew.");
        var concurrent = await Task.WhenAll(session.PostAsync<SportReply>(tryoutPath + "/decisions", withdrawal),
            session.PostAsync<SportReply>(tryoutPath + "/decisions", withdrawal with { OperationId = Guid.NewGuid(), Kind = DecisionKind.NotSelected }));
        concurrent.Count(result => result.Kind == SportReplyKind.Saved).ShouldBe(1);
        concurrent.Count(result => result.Kind == SportReplyKind.Conflict).ShouldBe(1);
        var current = await session.GetAsync<TryoutDetail>(tryoutPath);
        current.Notes.Count.ShouldBe(2);
        current.Notes.Single(value => value.Id == correction.Id).Text.ShouldBe(correction.Text);
        current.History.Count.ShouldBe(2);
        current.Roster.Single().CurrentTeamId.ShouldBeNull();
        var team = await session.GetAsync<TeamDetail>($"{path}/teams/{overview.Teams.Single().Id}");
        team.Members.ShouldBeEmpty();
        team.History.Count.ShouldBe(2);
        var roster = current.Roster.Single();
        var reopen = new DecisionInput(Guid.NewGuid(), roster.Player.Id, DecisionKind.Awaiting, TeamId: null, roster.Revision, roster.PlacementRevision, "Reopened for review.");
        (await session.PostAsync<SportReply>(tryoutPath + "/decisions", reopen)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/decisions", reopen)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/decisions", reopen with { Kind = DecisionKind.Withdrawn })).Kind.ShouldBe(SportReplyKind.Conflict);
        (await session.PostAsync<SportReply>(tryoutPath + "/decisions", reopen with { Reason = "Different reason under the same ID." })).Kind.ShouldBe(SportReplyKind.Conflict);
        var reopened = await session.GetAsync<TryoutDetail>(tryoutPath);
        reopened.Tryout.Complete.ShouldBeFalse();
        reopened.History.Count.ShouldBe(3);
        var tooYoung = new TeamInput { SeasonId = overview.Seasons.Single().Id, Name = "Graduation 2031", GraduationYear = 2031 };
        (await session.PostAsync<SportReply>(path + "/teams", tooYoung)).Kind.ShouldBe(SportReplyKind.Saved);
        var latest = reopened.Roster.Single();
        var invalid = new DecisionInput(Guid.NewGuid(), latest.Player.Id, DecisionKind.Placed, tooYoung.Id, latest.Revision, latest.PlacementRevision, "");
        (await session.PostAsync<SportReply>(tryoutPath + "/decisions", invalid)).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.GetAsync<TryoutDetail>(tryoutPath)).History.Count.ShouldBe(3);
    }

    private static async Task VerifyArchivesAndImportsAsync(BrowserSession session, string path, SportOverview overview)
    {
        var player = (await session.GetAsync<PlayerPage>(path + "/players?query=Jordan&archived=false&page=0")).Players.ShouldHaveSingleItem();
        var csv = $"PlayerReference,FirstName,LastName,GraduationYear,Position,ContactEmail\nNS-004,Alex,Reed,2030,,\n{player.PlayerReference},Duplicate,Player,2030,,";
        var import = await session.PostAsync<ImportReport>(path + "/import", new ImportInput(csv, Commit: true));
        import.Saved.ShouldBeFalse();
        import.Rows[^1].Error.ShouldNotBeNull();
        (await session.GetAsync<PlayerPage>(path + "/players?query=NS-004&archived=false&page=0")).Players.ShouldBeEmpty();
        const string Malformed = "PlayerReference,FirstName,LastName,GraduationYear,Position,ContactEmail\nWIDTH-1,Alex,Reed,2030,,\nWIDTH-2,Ada,Lee,2030,Midfield,,ada@example.test";
        var malformed = await session.PostAsync<ImportReport>(path + "/import", new ImportInput(Malformed, Commit: true));
        malformed.Saved.ShouldBeFalse();
        malformed.Rows[^1].Error.ShouldNotBeNull().ShouldContain("number of columns");
        (await session.GetAsync<PlayerPage>(path + "/players?query=WIDTH-&archived=false&page=0")).Players.ShouldBeEmpty();
        var season = overview.Seasons.Single();
        var input = new SeasonInput { Id = season.Id, Revision = season.Revision, Name = season.Name, StartsOn = season.StartsOn, EndsOn = season.EndsOn, Archived = true };
        (await session.PostAsync<SportReply>(path + "/seasons", input)).Kind.ShouldBe(SportReplyKind.Saved);
        var tryoutId = overview.Tryouts.Single().Id;
        var current = await session.GetAsync<TryoutDetail>($"{path}/tryouts/{tryoutId}");
        current.Season.Archived.ShouldBeTrue();
        (await session.PostAsync<SportReply>($"{path}/tryouts/{tryoutId}/bib", new BibNumberInput(current.Roster.Single().Player.Id, "41", "17"))).Kind.ShouldBe(SportReplyKind.Invalid);
        var note = new NoteInput(Guid.NewGuid(), current.Roster.Single().Player.Id, "Must not be written.", CorrectsId: null);
        (await session.PostAsync<SportReply>($"{path}/tryouts/{tryoutId}/notes", note)).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.GetAsync<TryoutDetail>($"{path}/tryouts/{tryoutId}")).Notes.Count.ShouldBe(2);
        input.Revision++;
        input.Archived = false;
        (await session.PostAsync<SportReply>(path + "/seasons", input)).Kind.ShouldBe(SportReplyKind.Saved);
    }
}
