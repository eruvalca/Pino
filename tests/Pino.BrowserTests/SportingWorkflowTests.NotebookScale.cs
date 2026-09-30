using System.Data.Common;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Playwright;
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
    [Theory(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    [InlineData(200)]
    [InlineData(1000)]
    public async Task LargeTryoutLoadsOnlySelectedPlayerObservationsAsync(int playerCount)
    {
        await using var session = await BrowserSession.CreateAsync();
        var email = await session.RegisterAsync();
        await session.CompleteProfileAsync(throughUi: false);
        var data = await PrepareCloseoutAsync(session);
        var queries = new NotebookQueries(data.Player.Id);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(Environment.GetEnvironmentVariable("PINO_TEST_DATABASE")).AddInterceptors(queries).Options;
        var factory = new CloseoutContextFactory(options);
        await using var db = factory.CreateDbContext();
        var ct = TestContext.Current.CancellationToken;
        var userId = await db.Users.Where(value => value.Email == email).Select(value => value.Id).SingleAsync(ct);
        await SeedNotebookRosterAsync(db, data, userId, playerCount - 3, ct);
        var actor = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "BrowserTest"));
        var service = new SportService(factory, Substitute.For<IProfilePhotoStore>(), TimeProvider.System);
        queries.Reads.Clear();
        var watch = Stopwatch.StartNew();
        var roster = await service.GetTryoutAsync(actor, data.ClubId, data.Tryout.Id, ct);
        roster.Roster.Count.ShouldBe(playerCount);
        queries.Reads.ShouldNotContain(sql => sql.Contains("\"DecisionEvents\"", StringComparison.Ordinal));
        var observationQuery = queries.Reads.Where(sql => sql.Contains("\"PlayerNotes\"", StringComparison.Ordinal)).ShouldHaveSingleItem();
        observationQuery.ShouldContain("SELECT DISTINCT");
        observationQuery.ShouldNotContain("\"Text\"");
        observationQuery.ShouldNotContain("\"Author\"");
        roster.Roster.Single(value => value.Player.Id == data.Player.Id).HasObservations.ShouldBeTrue();
        queries.UnscopedReads.Clear();
        var notebook = await service.GetNotebookAsync(actor, data.ClubId, data.Tryout.Id, data.Player.Id, ct);
        watch.Stop();
        notebook.Notes.ShouldHaveSingleItem().Text.ShouldBe(data.Note.Text);
        notebook.History.ShouldHaveSingleItem().PlayerId.ShouldBe(data.Player.Id);
        queries.UnscopedReads.ShouldBeEmpty("Personal history reads must bind the selected player in SQL.");
        TestContext.Current.TestOutputHelper?.WriteLine($"{playerCount} players: roster plus selected notebook read in {watch.ElapsedMilliseconds} ms; {queries.Reads.Count} queries.");

        var page = session.Page;
        await page.GotoAsync($"/clubs/{data.ClubId}/tryouts/{data.Tryout.Id}");
        await page.Locator(".roster-pages button[aria-label='Next players']:enabled").WaitForAsync();
        (await page.Locator(".roster-record").CountAsync()).ShouldBe(50);
        if (playerCount == 1000)
        {
            await session.CaptureAsync("thousand-player-roster-desktop");
            await page.SetViewportSizeAsync(390, 844);
            await session.CaptureAsync("thousand-player-roster-mobile");
            await page.GetByRole(AriaRole.Button, new() { Name = "Next players at bottom", Exact = true }).FocusAsync();
            await page.Keyboard.PressAsync("Enter");
            await page.Locator(".roster-pages-bottom").GetByText("Page 2", new() { Exact = true }).WaitForAsync();
            await page.Locator(".evaluation-roster h2:focus").WaitForAsync();
            await page.GetByRole(AriaRole.Button, new() { Name = "Previous players at bottom", Exact = true }).ClickAsync();
            await page.Locator(".roster-pages-bottom").GetByText("Page 1", new() { Exact = true }).WaitForAsync();
            await page.SetViewportSizeAsync(1440, 1000);
        }
        await page.GetByRole(AriaRole.Button, new() { Name = "Next players", Exact = true }).ClickAsync();
        await page.GetByLabel("Find a player", new() { Exact = true }).FillAsync("Jordan Rivera");
        await page.Locator(".roster-record").Filter(new() { HasText = "Jordan Rivera" }).ClickAsync();
        await page.GetByText(data.Note.Text, new() { Exact = true }).WaitForAsync();
        (await page.Locator(".roster-record").CountAsync()).ShouldBe(1);
        if (playerCount == 1000)
        {
            await session.CaptureAsync("thousand-player-notebook-desktop");
            await page.SetViewportSizeAsync(390, 844);
            await session.CaptureAsync("thousand-player-notebook-mobile");
            await page.SetViewportSizeAsync(1440, 1000);
            await page.GotoAsync($"/clubs/{data.ClubId}/tryouts/{data.Tryout.Id}/print");
            await page.Locator("#print-order:enabled").WaitForAsync();
            (await page.Locator(".roster-table tbody tr").CountAsync()).ShouldBe(1000);
            (await ReadCsvAsync(session, $"/api/clubs/{data.ClubId}/sport/tryouts/{data.Tryout.Id}/results.csv")).Count.ShouldBe(1000);
            (await page.Locator(".roster-sheet").InnerTextAsync()).ShouldNotContain("Synthetic observation");
            await page.EmulateMediaAsync(new() { Media = Media.Print });
            (await page.Locator(".roster-table tbody tr").CountAsync()).ShouldBe(1000);
            if (Environment.GetEnvironmentVariable("PINO_BROWSER_ARTIFACTS") is { Length: > 0 } output)
            {
                await page.PdfAsync(new() { Path = Path.Combine(output, "thousand-player-roster.pdf"), Format = "A4", Margin = new() { Top = "12mm", Bottom = "12mm", Left = "12mm", Right = "12mm" } });
            }
            await page.EmulateMediaAsync(new() { Media = Media.Screen });
        }
    }

    private static async Task SeedNotebookRosterAsync(ApplicationDbContext db, CloseoutFixture data, string actorId, int count, CancellationToken ct)
    {
        for (var number = 0; number < count; number++)
        {
            var playerId = Guid.NewGuid();
            db.Players.Add(new() { Id = playerId, ClubId = data.ClubId, PlayerReference = $"SYN-{number}", FirstName = $"Player {number}", LastName = "Synthetic", GraduationYear = 2030, Revision = 1 });
            db.Participations.Add(new() { ClubId = data.ClubId, TryoutId = data.Tryout.Id, PlayerId = playerId, Revision = 1 });
            db.SeasonPlacements.Add(new() { ClubId = data.ClubId, SeasonId = data.Season.Id, PlayerId = playerId });
            for (var observation = 0; observation < 4; observation++)
            {
                db.PlayerNotes.Add(new() { Id = Guid.NewGuid(), ClubId = data.ClubId, TryoutId = data.Tryout.Id, PlayerId = playerId, Text = $"Synthetic observation {observation} for player {number}.", AuthorId = actorId, Author = "Avery Coach", CreatedAt = DateTimeOffset.UtcNow });
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private sealed class NotebookQueries(Guid selectedPlayer) : DbCommandInterceptor
    {
        internal List<string> Reads { get; } = [];
        internal List<string> UnscopedReads { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData,
            InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (eventData.CommandSource == CommandSource.LinqQuery)
            {
                Reads.Add(command.CommandText);
                if ((command.CommandText.Contains("\"PlayerNotes\"", StringComparison.Ordinal) || command.CommandText.Contains("\"DecisionEvents\"", StringComparison.Ordinal)) &&
                    !command.Parameters.Cast<DbParameter>().Any(parameter => parameter.Value is Guid id && id == selectedPlayer)) { UnscopedReads.Add(command.CommandText); }
            }
            return ValueTask.FromResult(result);
        }
    }
}
