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
    private static async Task VerifyImportCommitAcknowledgementAsync(BrowserSession session, Guid clubId, string email)
    {
        var interceptor = new LostCommitAcknowledgement();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("PINO_TEST_DATABASE"), postgres => postgres.EnableRetryOnFailure(1, TimeSpan.Zero, errorCodesToAdd: null))
            .AddInterceptors(interceptor).Options;
        var factory = new ImportContextFactory(options);
        await using var db = factory.CreateDbContext();
        var userId = await db.Users.Where(user => user.Email == email).Select(user => user.Id).SingleAsync(TestContext.Current.CancellationToken);
        var actor = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "BrowserTest"));
        var service = new SportService(factory, Substitute.For<IProfilePhotoStore>(), TimeProvider.System);
        const string Csv = "PlayerReference,FirstName,LastName,GraduationYear,Position,ContactEmail\nRETRY-1,Ada,Lee,2030,Midfield,ada@example.test\nRETRY-2,Alex,Reed,2031,,";
        var report = await service.ImportAsync(actor, clubId, new(Csv, Commit: true), TestContext.Current.CancellationToken);
        interceptor.Commits.ShouldBe(2, "The real database commit must succeed, lose its acknowledgement, then replay once.");
        report.Saved.ShouldBeTrue();
        report.Rows.Count.ShouldBe(2);
        report.Rows.ShouldAllBe(row => row.Error == null);
        report.Message.ShouldBe("Imported 2 players.");
        var records = await session.GetAsync<PlayerPage>($"/api/clubs/{clubId}/sport/players?query=RETRY-&archived=false&page=0");
        records.Players.Select(player => player.PlayerReference).Order(StringComparer.Ordinal).ShouldBe(["RETRY-1", "RETRY-2"]);
        records.Players.Single(player => string.Equals(player.PlayerReference, "RETRY-1", StringComparison.Ordinal)).ContactEmail.ShouldBe("ada@example.test");
        // A new user submission is still a duplicate, not a replay of this invocation.
        var duplicate = await service.ImportAsync(actor, clubId, new(Csv, Commit: true), TestContext.Current.CancellationToken);
        duplicate.Saved.ShouldBeFalse();
        duplicate.Rows.ShouldAllBe(row => row.Error != null);
    }

    private sealed class ImportContextFactory(DbContextOptions<ApplicationDbContext> options) : IDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext() => new(options);
    }

    private sealed class LostCommitAcknowledgement : DbTransactionInterceptor
    {
        internal int Commits { get; private set; }

        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            Commits++;
            if (Commits == 1) { throw new TimeoutException("Injected acknowledgement loss after PostgreSQL committed."); }
            return Task.CompletedTask;
        }
    }
}
