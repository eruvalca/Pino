using Microsoft.Playwright;
using Shouldly;

namespace Pino.BrowserTests;

public sealed partial class SportingWorkflowTests
{
    private static async Task VerifyAccountNavigationAsync(BrowserSession session)
    {
        var page = session.Page;
        await page.GotoAsync("/Account/Manage");
        await page.GetByLabel("Phone number", new() { Exact = true }).FillAsync("3125550170");
        await page.GetByRole(AriaRole.Button, new() { Name = "Save", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Status).GetByText("Your profile has been updated").WaitForAsync();
        await page.ReloadAsync();
        (await page.GetByLabel("Phone number", new() { Exact = true }).InputValueAsync()).ShouldBe("3125550170");
        await CaptureReviewAsync(session, "account");
        await page.GetByRole(AriaRole.Link, new() { Name = "Edit name & photo", Exact = true }).ClickAsync();
        await page.GetByLabel("First name", new() { Exact = true }).WaitForAsync();
        (await page.GetByLabel("First name", new() { Exact = true }).InputValueAsync()).ShouldBe("Avery");

        await page.GotoAsync("/Account/Manage/Email?from=verification");
        // Blazor may focus the page heading after navigation. Reach the skip
        // link from that real focus position instead of assuming body focus.
        for (var step = 0; step < 12 && !string.Equals(await page.Locator(":focus").GetAttributeAsync("class"), "account-skip", StringComparison.Ordinal); step++)
        {
            await page.Keyboard.PressAsync("Shift+Tab");
        }
        (await page.Locator(":focus").GetAttributeAsync("class")).ShouldBe("account-skip");
        await page.Keyboard.PressAsync("Enter");
        page.Url.ShouldEndWith("/Account/Manage/Email?from=verification#account-content");
        (await page.Locator(":focus").GetAttributeAsync("id")).ShouldBe("account-content");

        await page.GotoAsync("/Account/Manage/EnableAuthenticator");
        (await page.Locator(".account-navigation [aria-current='page']").InnerTextAsync()).ShouldBe("Two-factor authentication");
        (await page.Locator(".authenticator-qr").GetAttributeAsync("src")).ShouldStartWith("data:image/png;base64,");
        (await page.Locator(".authenticator-key").InnerTextAsync()).ShouldNotBeNullOrWhiteSpace();
        await page.SetViewportSizeAsync(390, 844);
        (await page.Locator(".account-nav-group:visible").CountAsync()).ShouldBe(1);
        (await page.Locator(".account-section-tabs [aria-current='location']").InnerTextAsync()).ShouldBe("Security");
        (await page.EvaluateAsync<bool>("document.documentElement.scrollWidth <= window.innerWidth")).ShouldBeTrue();
        await page.GetByRole(AriaRole.Link, new() { Name = "Back to Two-factor authentication", Exact = true }).ClickAsync();
        await page.GetByText("Two-factor authentication is off.", new() { Exact = true }).WaitForAsync();
        await CaptureReviewAsync(session, "account-security");
        await page.SetViewportSizeAsync(390, 844);
        await page.GetByRole(AriaRole.Link, new() { Name = "Your data", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Heading, new() { Name = "Personal Data", Exact = true }).WaitForAsync();
        (await page.Locator(".account-navigation [aria-current='page']").InnerTextAsync()).ShouldBe("Personal data");
        await page.SetViewportSizeAsync(1440, 1000);
    }
}
