using Microsoft.Playwright;
using Pino.SharedKernel.Clubs;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task AccountAndClubShareNavigationAndPageWidthAsync()
    {
        await using var session = await BrowserSession.CreateAsync();
        await session.RegisterAsync();
        var page = session.Page;
        await page.SetViewportSizeAsync(390, 844);
        await page.Locator("#first-name:enabled").WaitForAsync();
        (await page.Locator(".club-context").CountAsync()).ShouldBe(0);
        await session.CaptureAsync("shell-no-membership-mobile");
        await session.CompleteProfileAsync(throughUi: false);
        const string ClubName = "Test · Northside Community Football Club — Youth Development & Academy";
        (await session.PostAsync<ClubReply>("/api/clubs/create", new CreateClubInput { Name = ClubName, Sport = "Soccer", City = "Chicago", State = "IL" })).Kind.ShouldBe(ClubReplyKind.Saved);
        var access = await session.GetAsync<AccessSnapshot>("/api/clubs/access");
        var clubId = access.Membership.ShouldNotBeNull().Club.Id;

        await page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        foreach (var width in new[] { 1993, 1440, 800, 390, 320 })
        {
            await page.SetViewportSizeAsync(width, width < 600 ? 844 : 1000);
            await page.GotoAsync("/Account/Manage");
            await page.GetByLabel("Phone number", new() { Exact = true }).WaitForAsync();
            var account = await ReadShellBoundsAsync(page, "main");
            var header = await ReadShellBoundsAsync(page, ".app-masthead");
            await CheckShellGeometryAsync(page, account.X, account.Width, header.Height, "Account");
            await session.CaptureAsync($"shell-account-{width}");

            foreach (var (route, name) in new[] { ($"/clubs/{clubId}", "overview"), ($"/clubs/{clubId}/players", "players"), ($"/clubs/{clubId}/seasons", "seasons"), ($"/clubs/{clubId}/people", "people"), ("/club/access", "access") })
            {
                await page.GotoAsync(route);
                await page.Locator("main h1").WaitForAsync();
                await page.WaitForFunctionAsync("!document.querySelector('main > article[aria-busy=true]')");
                await page.GetByRole(AriaRole.Link, new() { Name = "Your club: " + ClubName, Exact = true }).WaitForAsync();
                await CheckShellGeometryAsync(page, account.X, account.Width, header.Height, "Club workspace");
                var sheet = await ReadShellBoundsAsync(page, "main > article, main > section");
                Math.Abs(sheet.Width - account.Width).ShouldBeLessThan(1);
                if (name is "overview" || (width == 1440 && name is "people" or "access"))
                {
                    await page.EvaluateAsync("window.scrollTo(0, 0)");
                    await session.CaptureAsync($"shell-{name}-{width}");
                }
            }
        }

        await page.SetViewportSizeAsync(390, 844);
        await page.GetByRole(AriaRole.Link, new() { Name = "Players", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Heading, new() { Name = "Players", Exact = true }).WaitForAsync();
        await page.Locator("form:has(#player-search) button:enabled").WaitForAsync();
        await page.Locator(".site-name").FocusAsync();
        await page.Keyboard.PressAsync("Shift+Tab");
        (await page.Locator(":focus").GetAttributeAsync("class")).ShouldBe("skip-link");
        await page.Keyboard.PressAsync("Enter");
        (await page.Locator(":focus").GetAttributeAsync("id")).ShouldBe("club-content");
        await page.EvaluateAsync("window.scrollTo(0, 0)");
        await session.CaptureAsync("shell-keyboard-mobile");

        await page.GetByRole(AriaRole.Navigation, new() { Name = "Main navigation", Exact = true }).GetByRole(AriaRole.Link, new() { Name = "Account", Exact = true }).ClickAsync();
        await page.GetByLabel("Phone number", new() { Exact = true }).WaitForAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Club workspace", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Heading, new() { Name = ClubName, Exact = true, Level = 1 }).WaitForAsync();

        var logout = page.Locator(".navigation-links form");
        (await logout.GetAttributeAsync("method")).ShouldBe("post");
        (await logout.Locator("input[name='__RequestVerificationToken']").InputValueAsync()).ShouldNotBeNullOrWhiteSpace();
        await session.LogoutAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Log in", Exact = true }).WaitForAsync();
        (await page.Locator(".navigation-links").InnerTextAsync()).ShouldNotContain("Logout");
    }

    private static async Task CheckShellGeometryAsync(IPage page, float x, float width, float headerHeight, string selectedMode)
    {
        foreach (var selector in new[] { "main", ".app-masthead-inner" })
        {
            var bounds = await ReadShellBoundsAsync(page, selector);
            Math.Abs(bounds.X - x).ShouldBeLessThan(1);
            Math.Abs(bounds.Width - width).ShouldBeLessThan(1);
        }
        Math.Abs((await ReadShellBoundsAsync(page, ".app-masthead")).Height - headerHeight).ShouldBeLessThan(1);
        (await page.Locator(".navigation-links [aria-current]").InnerTextAsync()).ShouldBe(selectedMode);
        (await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth")).ShouldBeTrue();
    }

    private static async Task<(float X, float Width, float Height)> ReadShellBoundsAsync(IPage page, string selector)
    {
        // InteractiveAuto can replace prerendered elements while a route opens.
        // Wait for a laid-out element and read all dimensions in the same frame.
        var handle = await page.WaitForFunctionAsync("selector => { const rect = document.querySelector(selector)?.getBoundingClientRect(); return rect?.width > 0 && rect?.height > 0 ? [rect.x, rect.width, rect.height] : null; }", selector);
        var bounds = await handle.JsonValueAsync<float[]>();
        return (bounds[0], bounds[1], bounds[2]);
    }
}
