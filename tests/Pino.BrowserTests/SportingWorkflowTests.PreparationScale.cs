using System.Data.Common;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Playwright;
using NSubstitute;
using Pino.Data;
using Pino.Features.Clubs.Services;
using Pino.Features.Sporting.Models;
using Pino.Features.Sporting.Services;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task ThousandPlayerPreparationBatchesUseBoundedReadsAndPreservePreviousSeasonAsync()
    {
        await using var session = await BrowserSession.CreateAsync();
        var email = await session.RegisterAsync();
        await session.CompleteProfileAsync(throughUi: false);
        var data = await PrepareCloseoutAsync(session);
        var path = $"/api/clubs/{data.ClubId}/sport";
        var source = await session.GetAsync<TryoutDetail>($"{path}/tryouts/{data.Tryout.Id}");
        foreach (var entry in source.Roster.Where(value => value.Player.Id != data.Player.Id)) { await PlaceAsync(session, path, data.Tryout.Id, entry.Player.Id, data.Blue.Id); }
        var queries = new PreparationQueries();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(Environment.GetEnvironmentVariable("PINO_TEST_DATABASE")).AddInterceptors(queries).Options;
        var factory = new CloseoutContextFactory(options);
        await using var db = factory.CreateDbContext();
        var ct = TestContext.Current.CancellationToken;
        var actorId = await db.Users.Where(value => value.Email == email).Select(value => value.Id).SingleAsync(ct);
        for (var index = 0; index < 997; index++)
        {
            var id = Guid.NewGuid();
            db.Players.Add(new() { Id = id, ClubId = data.ClubId, PlayerReference = $"PREP-{index}", FirstName = $"Returning {index}", LastName = "Synthetic", GraduationYear = 2030, Revision = 1 });
            db.SeasonPlacements.Add(new() { ClubId = data.ClubId, SeasonId = data.Season.Id, PlayerId = id, TeamId = data.Blue.Id, TryoutId = data.Tryout.Id, Revision = 1 });
        }
        await db.SaveChangesAsync(ct);
        var actor = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, actorId)], "BrowserTest"));
        var service = new SportService(factory, Substitute.For<IProfilePhotoStore>(), TimeProvider.System);
        var season = new SeasonInput { Name = "Next season at scale", StartsOn = new(2028, 1, 1), EndsOn = new(2028, 6, 30) };
        (await session.PostAsync<SportReply>(path + "/seasons", season)).Kind.ShouldBe(SportReplyKind.Saved);
        (await session.GetAsync<SportOverview>(path + "/overview")).Seasons.Count.ShouldBe(2);
        var tryout = new TryoutInput { SeasonId = season.Id, Name = "Whole-club tryout", Date = new(2028, 2, 1) };
        queries.Reads = 0;
        (await service.SaveTryoutAsync(actor, data.ClubId, tryout, ct)).Value.ShouldBeOfType<SportOutcome.Saved>();
        queries.Reads.ShouldBeLessThan(15, "Creating a 1,000-player tryout must use set-based catalog and placement reads.");
        var automatic = await service.GetTryoutAsync(actor, data.ClubId, tryout.Id, ct);
        automatic.Roster.Count.ShouldBe(1000);
        automatic.Roster.ShouldAllBe(value => value.Decision == DecisionKind.Awaiting && value.Bib.Length == 0);
        var candidates = await service.GetEnrollmentCandidatesAsync(actor, data.ClubId, tryout.Id, ct);
        candidates.Count.ShouldBe(1000);
        var selection = candidates.Select(value => new EnrollmentSelection(value.Player.Id, value.Player.Revision)).ToArray();
        (await service.EnrollBulkAsync(actor, data.ClubId, tryout.Id, new(Guid.NewGuid(), []), ct)).Kind.ShouldBe(SportReplyKind.Invalid);
        (await service.EnrollBulkAsync(actor, data.ClubId, tryout.Id, new(Guid.NewGuid(), [selection[0], selection[0]]), ct)).Kind.ShouldBe(SportReplyKind.Invalid);
        (await service.EnrollBulkAsync(actor, data.ClubId, tryout.Id, new(Guid.NewGuid(), [.. selection, new(Guid.NewGuid(), 1)]), ct)).Kind.ShouldBe(SportReplyKind.Invalid);
        await session.Page.GotoAsync($"/clubs/{data.ClubId}/tryouts/{tryout.Id}/enrollments");
        await session.Page.GetByRole(AriaRole.Button, new() { Name = "Select all matching", Exact = true }).ClickAsync();
        (await session.Page.Locator(".record-list input[type='checkbox']").CountAsync()).ShouldBe(50);
        await session.Page.GetByRole(AriaRole.Button, new() { Name = "Review players to exclude", Exact = true }).ClickAsync();
        await session.Page.GetByRole(AriaRole.Heading, new() { Name = "Exclude 1000 players", Exact = true }).WaitForAsync();
        await CapturePreparationAsync(session, "group-exclusion-review");
        await session.Page.GetByRole(AriaRole.Button, new() { Name = "Cancel", Exact = true }).ClickAsync();
        queries.Reads = 0;
        var batch = new BulkEnrollmentInput(Guid.NewGuid(), selection);
        var enrolled = await service.EnrollBulkAsync(actor, data.ClubId, tryout.Id, batch, ct);
        enrolled.Applied.ShouldBe(0);
        enrolled.Skipped.ShouldBe(1000);
        queries.Reads.ShouldBeLessThan(20, "Bulk enrollment must not query each player separately.");
        var returning = await service.GetReturningPlayersAsync(actor, data.ClubId, tryout.Id, data.Season.Id, ct);
        returning.Count.ShouldBe(1000);
        returning.ShouldAllBe(value => value.Selection != null);
        queries.Reads = 0;
        var placementBatch = new ReturningPlacementInput(Guid.NewGuid(), data.Season.Id, returning.Select(value => value.Selection!).ToArray());
        var placed = await service.PlaceReturningPlayersAsync(actor, data.ClubId, tryout.Id, placementBatch, ct);
        placed.Applied.ShouldBe(1000);
        placed.Skipped.ShouldBe(0);
        queries.Reads.ShouldBeLessThan(30, "Returning placement must not query each player separately.");
        (await service.EnrollBulkAsync(actor, data.ClubId, tryout.Id, batch, ct)).Replayed.ShouldBeTrue();
        (await service.GetTryoutAsync(actor, data.ClubId, tryout.Id, ct)).Roster.ShouldAllBe(value => value.CurrentTeamId == data.Blue.Id && value.Decision == DecisionKind.Placed);
        (await service.PlaceReturningPlayersAsync(actor, data.ClubId, tryout.Id, placementBatch, ct)).Replayed.ShouldBeTrue();
        (await db.SeasonPlacements.CountAsync(value => value.ClubId == data.ClubId && value.SeasonId == data.Season.Id && value.TeamId == data.Blue.Id, ct)).ShouldBe(1000);
        (await db.DecisionEvents.CountAsync(value => value.ClubId == data.ClubId && value.TryoutId == tryout.Id, ct)).ShouldBe(1000);
    }

    private sealed class PreparationQueries : DbCommandInterceptor
    {
        internal int Reads { get; set; }
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (eventData.CommandSource == CommandSource.LinqQuery) { Reads++; }
            return ValueTask.FromResult(result);
        }
    }
}
