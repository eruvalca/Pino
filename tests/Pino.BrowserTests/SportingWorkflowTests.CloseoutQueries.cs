using System.Data.Common;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using Pino.Data;
using Pino.Features.Clubs.Services;
using Pino.Features.Sporting.Services;
using Pino.SharedKernel.Sporting;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task ClosingTryoutReadsCurrentResultsWithoutLoadingHistoricalRostersAsync()
    {
        await using var session = await BrowserSession.CreateAsync();
        var email = await session.RegisterAsync();
        await session.CompleteProfileAsync(throughUi: false);
        var data = await PrepareCloseoutAsync(session);
        var path = $"/api/clubs/{data.ClubId}/sport/tryouts/{data.Tryout.Id}";
        for (var edition = 0; edition < 2; edition++)
        {
            var review = await session.GetAsync<TryoutReview>(path + "/review");
            var id = Guid.NewGuid();
            (await session.PostAsync<SportReply>(path + "/close", new CloseTryoutInput(id, review.ReviewToken))).Kind.ShouldBe(SportReplyKind.Saved);
            (await session.PostAsync<SportReply>(path + "/reopen", new ReopenTryoutInput(id, "Review a follow-up observation."))).Kind.ShouldBe(SportReplyKind.Saved);
        }

        var queries = new CloseoutQueries();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("PINO_TEST_DATABASE"))
            .AddInterceptors(queries).Options;
        var factory = new CloseoutContextFactory(options);
        await using var db = factory.CreateDbContext();
        var userId = await db.Users.Where(user => user.Email == email).Select(user => user.Id).SingleAsync(TestContext.Current.CancellationToken);
        var actor = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "BrowserTest"));
        var service = new SportService(factory, Substitute.For<IProfilePhotoStore>(), TimeProvider.System);
        var current = await service.GetTryoutReviewAsync(actor, data.ClubId, data.Tryout.Id, TestContext.Current.CancellationToken);
        current.Closeouts.Count.ShouldBe(2);
        queries.Reads.ShouldContain(sql => sql.Contains("\"TryoutCloseoutPlayers\"", StringComparison.Ordinal));
        queries.Reads.Clear();

        var operationId = Guid.NewGuid();
        var result = await service.CloseTryoutAsync(actor, data.ClubId, data.Tryout.Id,
            new(operationId, current.ReviewToken), TestContext.Current.CancellationToken);
        result.ToReply().Kind.ShouldBe(SportReplyKind.Saved);
        result.ToReply().Id.ShouldBe(operationId);
        queries.Reads.ShouldContain(sql => sql.Contains("\"Participations\"", StringComparison.Ordinal));
        queries.Reads.ShouldNotContain(sql => sql.Contains("\"TryoutCloseoutPlayers\"", StringComparison.Ordinal),
            "Closing must not materialize earlier editions while holding the shared write lock.");

        var saved = await session.GetAsync<TryoutReview>(path + "/review");
        saved.Tryout.Closed.ShouldBeTrue();
        saved.Closeouts.Count.ShouldBe(3);
        saved.Closeouts.Single(value => value.Id == operationId).Results.ShouldBe(current.Results);
        foreach (var earlier in current.Closeouts)
        {
            saved.Closeouts.Single(value => value.Id == earlier.Id).Results.ShouldBe(earlier.Results);
        }
    }

    private sealed class CloseoutContextFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }

    private sealed class CloseoutQueries : DbCommandInterceptor
    {
        internal List<string> Reads { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (eventData.CommandSource == CommandSource.LinqQuery) { Reads.Add(command.CommandText); }
            return ValueTask.FromResult(result);
        }
    }
}
