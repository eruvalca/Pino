using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Pino.SharedKernel.Clubs;
using Shouldly;
using Xunit;

namespace Pino.BrowserTests;

public sealed partial class ClubAdministrationTests
{
    [Fact(Skip = SkipReason, SkipUnless = nameof(BrowserEnvironment.Enabled), SkipType = typeof(BrowserEnvironment))]
    public async Task PublicIntroductionConnectsSampleSignupSetupAndPasswordLockoutRecoveryAsync()
    {
        await using var session = await BrowserSession.CreateAsync();
        var page = session.Page;
        await page.GotoAsync("/");
        await page.GetByRole(AriaRole.Heading, new() { Name = "Your club's tryout notebook.", Exact = true }).WaitForAsync();
        await CaptureAsync(session, "public-introduction");
        await page.GetByRole(AriaRole.Link, new() { Name = "Try the sample tryout", Exact = true }).FocusAsync();
        await page.Keyboard.PressAsync("Enter");
        await page.GetByText("Fictional data · Changes last until you leave or reload", new() { Exact = true }).WaitForAsync();
        await page.GotoAsync("/guide");
        await page.GetByRole(AriaRole.Heading, new() { Name = "Staff guide", Exact = true }).WaitForAsync();
        await CaptureAsync(session, "staff-guide");
        await page.GetByRole(AriaRole.Link, new() { Name = "Make decisions", Exact = true }).ClickAsync();
        new Uri(page.Url).Fragment.ShouldBe("#decisions");
        await page.GotoAsync("/Account/Register");
        var email = session.CreateEmail();
        await session.SubmitRegistrationAsync(email, adultStaff: false);
        await page.GetByText("Confirm that you are an adult acting as club staff.", new() { Exact = true }).First.WaitForAsync();
        await CaptureAsync(session, "staff-registration");
        await using var db = Database();
        (await db.Users.AnyAsync(value => value.Email == email, Token)).ShouldBeFalse();
        await session.RegisterAsync(email);
        new Uri(page.Url).AbsolutePath.ShouldBe("/club/access");
        await session.CompleteProfileAsync(throughUi: false);
        (await session.PostAsync<ClubReply>("/api/clubs/create", new CreateClubInput { Name = "Test · New club", Sport = "Soccer", City = "Chicago", State = "IL" })).Succeeded.ShouldBeTrue();
        var club = (await session.GetAsync<AccessSnapshot>("/api/clubs/access")).Membership.ShouldNotBeNull().Club.Id;
        await page.GotoAsync($"/clubs/{club}");
        await page.GetByText("To do · Player list", new() { Exact = true }).WaitForAsync();
        await CaptureAsync(session, "club-setup-checklist");
        await page.ReloadAsync();
        await page.GetByText("To do · Player list", new() { Exact = true }).WaitForAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Logout", Exact = true }).ClickAsync();
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            await page.GotoAsync("/Account/Login");
            await session.SubmitPasswordAsync(email, "Incorrect-Password-123!");
            if (attempt < 5) { await page.GetByText("Error: We could not sign you in. Check your email and password, then try again.", new() { Exact = true }).WaitForAsync(); }
        }
        await page.WaitForURLAsync("**/Account/Lockout");
        var user = await db.Users.AsNoTracking().SingleAsync(value => value.Email == email, Token);
        user.LockoutEnd.ShouldNotBeNull().ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddMinutes(14));
        await CaptureAsync(session, "account-lockout");
        // Advance only this disposable account's lockout; do not wait fifteen real minutes.
        await db.Users.Where(value => value.Id == user.Id).ExecuteUpdateAsync(update => update.SetProperty(value => value.LockoutEnd, DateTimeOffset.UtcNow.AddSeconds(-1)), Token);
        await session.LoginAsync(email, BrowserSession.Password);
        new Uri(page.Url).AbsolutePath.ShouldBe("/club/access");
        (await db.Users.AsNoTracking().SingleAsync(value => value.Id == user.Id, Token)).AccessFailedCount.ShouldBe(0);
    }
}
