using Microsoft.Playwright;
using Pino.SharedKernel.Sporting;
using Shouldly;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    private static async Task CaptureEntrySurfacesAsync(BrowserSession session)
    {
        foreach (var (path, name) in new[] { ("/Account/Register", "registration"), ("/Account/Login", "login"), ("/Account/ForgotPassword", "password-recovery") })
        {
            await session.Page.GotoAsync(path);
            await session.Page.Locator("main h1").WaitForAsync();
            await CaptureSurfaceSizesAsync(session, name);
        }
    }

    private static async Task CaptureEnrollmentAsync(BrowserSession session, Guid clubId, Guid tryoutId, PlayerPage players)
    {
        await session.Page.GotoAsync($"/clubs/{clubId}/tryouts/{tryoutId}");
        await session.Page.Locator(".tryout-heading .primary-action:not([disabled])").WaitForAsync();
        await session.Page.GetByRole(AriaRole.Button, new() { Name = "Add players", Exact = true }).ClickAsync();
        await session.Page.GetByRole(AriaRole.Heading, new() { Name = "Add players from your catalog", Exact = true }).WaitForAsync();
        var search = (await session.Page.Locator("#enroll-search").BoundingBoxAsync()).ShouldNotBeNull();
        var bib = (await session.Page.Locator("#enroll-bib").BoundingBoxAsync()).ShouldNotBeNull();
        Math.Abs(search.Y - bib.Y).ShouldBeLessThan(1);
        var content = await session.Page.Locator("main").InnerTextAsync();
        foreach (var player in players.Players) { content.ShouldNotContain(player.PlayerReference); }
        await CaptureSurfaceSizesAsync(session, "enrollment");
    }

    private static async Task CaptureRemainingSurfacesAsync(BrowserSession session, Guid clubId)
    {
        var overview = await session.GetAsync<SportOverview>($"/api/clubs/{clubId}/sport/overview");
        var routes = new List<(string Path, string Name)>
        {
            ($"/clubs/{clubId}", "club-overview"),
            ($"/clubs/{clubId}/people", "club-people"),
            ("/club/access", "club-access"),
            ($"/clubs/{clubId}/players/import", "player-import-empty"),
            ($"/clubs/{clubId}/seasons", "seasons"),
            ($"/clubs/{clubId}/teams/{overview.Teams[0].Id}", "team-roster"),
            ("/Account/Manage/Email", "account-email"),
            ("/Account/Manage/ChangePassword", "account-password"),
            ("/Account/Manage/Passkeys", "account-passkeys"),
            ("/Account/Manage/ExternalLogins", "account-connections"),
            ("/Account/Manage/PersonalData", "account-data"),
        };
        foreach (var (path, name) in routes)
        {
            await session.Page.GotoAsync(path);
            await session.Page.Locator("main h1").WaitForAsync();
            await CaptureSurfaceSizesAsync(session, name);
        }
        await session.Page.GotoAsync("/tryouts/spring-2027");
        await session.Page.Locator(".player-row").First.WaitForAsync();
        await session.CaptureAsync("sample-roster-desktop");
        await session.Page.SetViewportSizeAsync(390, 844);
        await session.Page.GetByRole(AriaRole.Button, new() { Name = "Roster", Exact = true }).ClickAsync();
        await session.Page.Locator(".player-row").First.WaitForAsync();
        await session.CaptureAsync("sample-roster-mobile");
        await session.Page.Locator(".player-row").First.ClickAsync();
        await session.Page.Locator(".notebook-region .player-heading").WaitForAsync();
        await session.CaptureAsync("sample-notebook-mobile");
        await session.Page.SetViewportSizeAsync(1440, 1000);
    }

    private static async Task CaptureSurfaceSizesAsync(BrowserSession session, string name)
    {
        foreach (var (width, height, suffix) in new[] { (1440, 1000, "desktop"), (390, 844, "mobile") })
        {
            await session.Page.SetViewportSizeAsync(width, height);
            await session.Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
            await session.Page.EvaluateAsync("window.scrollTo(0, 0)");
            await session.CaptureAsync(name + "-" + suffix);
        }
        await session.Page.SetViewportSizeAsync(1440, 1000);
        await session.Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.NoPreference });
    }
}
