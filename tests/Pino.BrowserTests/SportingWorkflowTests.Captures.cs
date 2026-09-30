using Microsoft.Playwright;
using Pino.SharedKernel.Sporting;
using Shouldly;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    private static async Task CaptureCompletedWorkspaceAsync(BrowserSession session, Guid clubId, Guid tryoutId)
    {
        var path = $"/api/clubs/{clubId}/sport";
        var rosterPath = $"{path}/tryouts/{tryoutId}";
        var players = await session.GetAsync<PlayerPage>(path + "/players?query=&archived=false&page=0");
        await CaptureEnrollmentAsync(session, clubId, tryoutId, players);
        foreach (var player in players.Players.Where(player => !string.Equals(player.FirstName, "Jordan", StringComparison.Ordinal)))
        {
            (await session.PostAsync<SportReply>(rosterPath + "/players", new EnrollmentInput(player.Id, string.Equals(player.PlayerReference, "NS-002", StringComparison.Ordinal) ? "22" : "35"))).Kind.ShouldBe(SportReplyKind.Saved);
        }
        var detail = await session.GetAsync<TryoutDetail>(rosterPath);
        detail.Tryout.Complete.ShouldBeFalse();
        await session.Page.GotoAsync($"/clubs/{clubId}/tryouts/{tryoutId}");
        await session.Page.Locator("#player-note:enabled").WaitForAsync();
        await session.Page.GetByRole(AriaRole.Button, new() { Name = "17 Jordan Rivera" }).ClickAsync();
        await session.CaptureAsync("tryout-desktop");
        await session.Page.SetViewportSizeAsync(390, 844);
        await session.CaptureAsync("tryout-mobile-notebook");
        await session.Page.GetByRole(AriaRole.Button, new() { Name = "Back to roster", Exact = true }).ClickAsync();
        await session.CaptureAsync("tryout-mobile-roster");
        await session.Page.GotoAsync($"/clubs/{clubId}/players");
        await session.Page.Locator(".sport-toolbar button:enabled").WaitForAsync();
        await session.Page.GetByRole(AriaRole.Link, new() { Name = "Jordan Rivera", Exact = true }).WaitForAsync();
        await session.CaptureAsync("players-mobile");
        await session.Page.SetViewportSizeAsync(1440, 1000);
        await session.CaptureAsync("players-desktop");
        await VerifyAccountNavigationAsync(session);
        await CaptureRemainingSurfacesAsync(session, clubId);
    }
}
