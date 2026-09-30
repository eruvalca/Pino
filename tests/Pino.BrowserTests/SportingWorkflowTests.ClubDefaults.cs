using Microsoft.EntityFrameworkCore;
using Pino.Data;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task AutomaticRosterAndGroupExclusionsPreserveHistoryDetectConflictsAndRejectOverflowAsync()
    {
        await using var session = await BrowserSession.CreateAsync();
        await session.RegisterAsync();
        await session.CompleteProfileAsync(throughUi: false);
        var data = await PrepareCloseoutAsync(session);
        var path = $"/api/clubs/{data.ClubId}/sport";
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(Environment.GetEnvironmentVariable("PINO_TEST_DATABASE")).Options);
        var ct = TestContext.Current.CancellationToken;
        for (var index = 0; index < 197; index++)
        {
            db.Players.Add(new() { ClubId = data.ClubId, PlayerReference = $"DEFAULT-{index}", FirstName = $"Player {index}", LastName = "Synthetic", GraduationYear = 2030, Revision = 1 });
        }
        var archivedId = Guid.NewGuid();
        db.Players.Add(new() { Id = archivedId, ClubId = data.ClubId, PlayerReference = "ARCHIVED-DEFAULT", FirstName = "Archived", LastName = "Synthetic", GraduationYear = 2030, Archived = true, Revision = 1 });
        await db.SaveChangesAsync(ct);
        var target = new SeasonInput { Name = "Automatic roster season", StartsOn = new(2028, 1, 1), EndsOn = new(2028, 6, 30) };
        (await session.PostAsync<SportReply>(path + "/seasons", target)).Kind.ShouldBe(SportReplyKind.Saved);
        var tryout = new TryoutInput { SeasonId = target.Id, Name = "All active players", Date = new(2028, 2, 1) };
        (await session.PostAsync<SportReply>(path + "/tryouts", tryout)).Kind.ShouldBe(SportReplyKind.Saved);
        var tryoutPath = $"{path}/tryouts/{tryout.Id}";
        var initial = await session.GetAsync<TryoutDetail>(tryoutPath);
        initial.Roster.Count.ShouldBe(200);
        initial.Roster.ShouldNotContain(value => value.Player.Id == archivedId);
        initial.Roster.ShouldAllBe(value => value.Bib.Length == 0 && value.Decision == DecisionKind.Awaiting);
        var playerId = data.Player.Id;
        await PlaceAsync(session, path, tryout.Id, playerId, data.Blue.Id);
        (await session.PostAsync<SportReply>(tryoutPath + "/notes", new NoteInput(Guid.NewGuid(), playerId, "Keep this observation in exclusion history"))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/attendance", new AttendanceInput(playerId, AttendanceKind.Present, 0))).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.PostAsync<SportReply>(tryoutPath + "/bib", new BibNumberInput(playerId, "17", ""))).Kind.ShouldBe(SportReplyKind.Saved);
        var ready = await session.GetAsync<TryoutDetail>(tryoutPath);
        var first = ready.Roster.Single(value => value.Player.Id == playerId);
        var changed = ready.Roster.First(value => value.Player.Id != playerId);
        var rows = new[] { new EnrollmentChangeSelection(playerId, first.Revision, first.PlacementRevision), new(changed.Player.Id, changed.Revision, changed.PlacementRevision) };
        var batch = new BulkEnrollmentChangeInput(Guid.NewGuid(), true, "Attend a separate tryout", rows);
        (await session.PostAsync<SportingBatchReport>(tryoutPath + "/enrollments/batch", batch with { Reason = "" })).Kind.ShouldBe(SportReplyKind.Invalid);
        (await session.PostAsync<SportReply>(tryoutPath + "/decisions", new DecisionInput(Guid.NewGuid(), changed.Player.Id, DecisionKind.NotSelected, null, changed.Revision, changed.PlacementRevision, "Concurrent staff decision"))).Kind.ShouldBe(SportReplyKind.Saved);
        var excluded = await session.PostAsync<SportingBatchReport>(tryoutPath + "/enrollments/batch", batch);
        excluded.Applied.ShouldBe(1);
        excluded.Skipped.ShouldBe(1);
        excluded.Rows.Single(value => value.PlayerId == changed.Player.Id).Applied.ShouldBeFalse();
        (await session.PostAsync<SportingBatchReport>(tryoutPath + "/enrollments/batch", batch)).Replayed.ShouldBeTrue();
        var notebook = await session.GetAsync<PlayerNotebook>($"{tryoutPath}/players/{playerId}/notebook");
        notebook.Removed.ShouldBeTrue();
        notebook.Notes.ShouldHaveSingleItem().Text.ShouldBe("Keep this observation in exclusion history");
        notebook.Attendance.ShouldHaveSingleItem().Kind.ShouldBe(AttendanceKind.Present);
        (await session.GetAsync<TeamDetail>($"{path}/teams/{data.Blue.Id}?seasonId={target.Id}")).Members.ShouldBeEmpty();
        (await session.GetAsync<TeamDetail>($"{path}/teams/{data.Blue.Id}?seasonId={data.Season.Id}")).Members.ShouldHaveSingleItem().Id.ShouldBe(playerId);
        var late = new PlayerInput { FirstName = "Late", LastName = "Addition", GraduationYear = 2030 };
        (await session.PostAsync<SportReply>(path + "/players", late)).Kind.ShouldBe(SportReplyKind.Saved);
        var current = await session.GetAsync<TryoutDetail>(tryoutPath);
        (await session.PostAsync<SportReply>(path + "/tryouts", new TryoutInput { Id = tryout.Id, SeasonId = target.Id, Name = "Edited tryout", Date = tryout.Date, Revision = current.Tryout.Revision })).Kind.ShouldBe(SportReplyKind.Saved);
        var edited = await session.GetAsync<TryoutDetail>(tryoutPath);
        edited.Roster.Count.ShouldBe(199);
        edited.Roster.ShouldNotContain(value => value.Player.Id == late.Id || value.Player.Id == playerId);
        await VerifyGroupRestorationAsync(session, tryoutPath, playerId, changed.Player.Id);
        var candidates = await session.GetAsync<EnrollmentCandidate[]>(tryoutPath + "/enrollment-candidates");
        var lateCandidate = candidates.Single(value => value.Player.Id == late.Id);
        var includeLate = new BulkEnrollmentInput(Guid.NewGuid(), [new(late.Id, lateCandidate.Player.Revision)]);
        var concurrent = await Task.WhenAll(session.PostAsync<SportingBatchReport>(tryoutPath + "/bulk-enrollment", includeLate), session.PostAsync<SportingBatchReport>(tryoutPath + "/bulk-enrollment", includeLate));
        concurrent.ShouldAllBe(value => value.Applied == 1 && value.Skipped == 0 && value.Kind == SportReplyKind.Saved);
        concurrent.Count(value => value.Replayed).ShouldBe(1);
        for (var index = 0; index < 1800; index++)
        {
            db.Players.Add(new() { ClubId = data.ClubId, PlayerReference = $"OVERFLOW-{index}", FirstName = $"Overflow {index}", LastName = "Synthetic", GraduationYear = 2030, Revision = 1 });
        }
        await db.SaveChangesAsync(ct);
        var overflow = new TryoutInput { SeasonId = target.Id, Name = "Must not be partially created", Date = tryout.Date };
        (await session.PostAsync<SportReply>(path + "/tryouts", overflow)).Kind.ShouldBe(SportReplyKind.Invalid);
        (await db.TryoutEvents.AnyAsync(value => value.Id == overflow.Id, ct)).ShouldBeFalse();
        (await db.Participations.AnyAsync(value => value.TryoutId == overflow.Id, ct)).ShouldBeFalse();
        (await session.GetAsync<TryoutDetail>(tryoutPath)).Roster.Count.ShouldBe(201);
    }

    private static async Task VerifyGroupRestorationAsync(BrowserSession session, string path, Guid playerId, Guid bibHolder)
    {
        (await session.PostAsync<SportReply>(path + "/bib", new BibNumberInput(bibHolder, "17", ""))).Kind.ShouldBe(SportReplyKind.Saved);
        var removed = (await session.GetAsync<EnrollmentDetail[]>(path + "/enrollments")).Single(value => value.Entry.Player.Id == playerId);
        removed.History.ShouldHaveSingleItem().Reason.ShouldBe("Attend a separate tryout");
        var restore = new BulkEnrollmentChangeInput(Guid.NewGuid(), false, "Confirmed participation", [new(playerId, removed.Entry.Revision, removed.Entry.PlacementRevision)]);
        var conflict = await session.PostAsync<SportingBatchReport>(path + "/enrollments/batch", restore);
        conflict.Applied.ShouldBe(0);
        conflict.Rows.ShouldHaveSingleItem().Message.ShouldContain("bib is in use");
        (await session.PostAsync<SportReply>(path + "/bib", new BibNumberInput(bibHolder, "", "17"))).Kind.ShouldBe(SportReplyKind.Saved);
        var saved = await session.PostAsync<SportingBatchReport>(path + "/enrollments/batch", restore with { OperationId = Guid.NewGuid() });
        saved.Applied.ShouldBe(1);
        saved.Skipped.ShouldBe(0);
        var entry = (await session.GetAsync<TryoutDetail>(path)).Roster.Single(value => value.Player.Id == playerId);
        entry.Decision.ShouldBe(DecisionKind.Awaiting);
        entry.CurrentTeamId.ShouldBeNull();
        entry.Bib.ShouldBe("17");
        (await session.GetAsync<EnrollmentDetail[]>(path + "/enrollments")).Single(value => value.Entry.Player.Id == playerId).History.Count.ShouldBe(2);
    }
}
