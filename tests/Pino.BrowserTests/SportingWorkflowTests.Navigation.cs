using Microsoft.Playwright;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task EnhancedNavigationKeepsClubContentThroughColdAndWarmStartupAsync()
    {
        await using var session = await BrowserSession.CreateAsync();
        await session.RegisterAsync();
        await session.CompleteProfileAsync(throughUi: false);
        var data = await PrepareCloseoutAsync(session);
        var page = session.Page;
        await page.AddInitScriptAsync("""
            window.pinoNavigationProbe = { documentId: crypto.randomUUID(), loading: [], missingClub: [], samples: 0 };
            document.addEventListener('DOMContentLoaded', () => {
                let sawClub = false;
                const inspect = () => {
                    if (!location.pathname.startsWith('/clubs/')) return;
                    const probe = window.pinoNavigationProbe;
                    probe.samples++;
                    const club = document.querySelector('.club-context');
                    if (club) sawClub = true;
                    else if (sawClub) probe.missingClub.push(location.pathname);
                    const heading = document.querySelector('main h1')?.textContent ?? '';
                    if (heading.includes('Opening your workspace')) probe.loading.push(location.pathname);
                };
                new MutationObserver(inspect).observe(document.body, { childList: true, subtree: true });
                inspect();
            });
            """);
        await page.GotoAsync($"/clubs/{data.ClubId}/tryouts/{data.Tryout.Id}");
        await page.Locator(".roster-record:enabled").First.WaitForAsync();
        (await page.Locator(".sport-sheet").GetAttributeAsync("data-renderer")).ShouldBeOneOf("Server", "WebAssembly");
        await VerifyClubNavigationAsync(session, data.ClubId);
        // Wait for Auto's background runtime download before a fresh, warm navigation.
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.ReloadAsync();
        await page.Locator(".sport-sheet[data-renderer='WebAssembly']").WaitForAsync();
        await VerifyClubNavigationAsync(session, data.ClubId);
        await page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await page.SetViewportSizeAsync(390, 844);
        await VerifyClubNavigationAsync(session, data.ClubId);
        (await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= innerWidth")).ShouldBeTrue();
        await session.CaptureAsync("navigation-reduced-motion-mobile");
        await page.GetByRole(AriaRole.Link, new() { Name = "Account", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Heading, new() { Name = "Profile", Exact = true }).WaitForAsync();
        (await page.Locator("form[method='post'] input[name='__RequestVerificationToken']").CountAsync()).ShouldBeGreaterThan(0);
        await page.GetByRole(AriaRole.Link, new() { Name = "Club workspace", Exact = true }).ClickAsync();
        await page.Locator(".club-context").WaitForAsync();
    }

    private static async Task VerifyClubNavigationAsync(BrowserSession session, Guid clubId)
    {
        var page = session.Page;
        var documentId = await page.EvaluateAsync<string>("window.pinoNavigationProbe.documentId");
        await page.GetByRole(AriaRole.Link, new() { Name = "Players", Exact = true }).ClickAsync();
        await page.Locator("form:has(#player-search) button:enabled").WaitForAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "Seasons & teams", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Add season", Exact = true }).WaitForAsync();
        await page.GoBackAsync();
        await page.Locator("form:has(#player-search) button:enabled").WaitForAsync();
        await page.GoForwardAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Add season", Exact = true }).WaitForAsync();
        var workspace = page.GetByRole(AriaRole.Link, new() { Name = "Club workspace", Exact = true });
        (await workspace.GetAttributeAsync("href")).ShouldBe($"/clubs/{clubId}");
        await workspace.FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.Locator(".sport-sheet[aria-busy='false']").WaitForAsync();
        new Uri(page.Url).AbsolutePath.ShouldBe($"/clubs/{clubId}");
        (await page.EvaluateAsync<string>("window.pinoNavigationProbe.documentId")).ShouldBe(documentId, "club links and history should use enhanced navigation");
        (await page.EvaluateAsync<int>("window.pinoNavigationProbe.samples")).ShouldBeGreaterThan(0);
        (await page.EvaluateAsync<string[]>("window.pinoNavigationProbe.loading")).ShouldBeEmpty("prerendered club content must not revert to a loading screen during hydration");
        (await page.EvaluateAsync<string[]>("window.pinoNavigationProbe.missingClub")).ShouldBeEmpty("the club navigation must remain present during club navigation");
    }
}
