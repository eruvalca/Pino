using System.Diagnostics.CodeAnalysis;
using Bunit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Pino.Features.Account.Components;
using Pino.UI.Layout;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Account;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class AccountNavigationTests
{
    [Theory]
    [InlineData("Account/Manage", "profile", "profile")]
    [InlineData("Account/Manage/Email", "email", "profile")]
    [InlineData("Account/Manage/SetPassword", "password", "security")]
    [InlineData("Account/Manage/ExternalLogins", "external", "security")]
    [InlineData("Account/Manage/EnableAuthenticator", "twofactor", "security")]
    [InlineData("Account/Manage/ResetAuthenticator", "twofactor", "security")]
    [InlineData("Account/Manage/Disable2fa", "twofactor", "security")]
    [InlineData("Account/Manage/GenerateRecoveryCodes", "twofactor", "security")]
    [InlineData("Account/Manage/RenamePasskey/credential", "passkeys", "security")]
    [InlineData("account/manage/renamepasskey/key?returnUrl=/Account/Manage/Email#section", "passkeys", "security")]
    [InlineData("Account/Manage/DeletePersonalData", "data", "data")]
    public async Task ChildRoutesKeepTheirParentSelectedAsync(string route, string section, string group)
    {
        await using var context = new BunitContext();
        var account = context.ConfigureAccount();
        account.SignIn.GetExternalAuthenticationSchemesAsync().Returns([
            new AuthenticationScheme("oidc", "Work account", typeof(IAuthenticationHandler))]);
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/" + route);
        var menu = account.Render<ManageNavMenu>(context);
        var selected = menu.FindAll("[aria-current='page']").ShouldHaveSingleItem();
        selected.GetAttribute("href").ShouldBe(AccountNavigation.Href(section));
        selected.TextContent.ShouldBe(AccountNavigation.Label(section));
        AccountNavigation.Group(section).ShouldBe(group);
        menu.FindAll(".account-nav-group[data-current='true']").ShouldHaveSingleItem();
        menu.FindAll(".account-nav-group[data-current='false']").Count.ShouldBe(2);
    }

    [Fact]
    public async Task SkipLinkRetainsTheCurrentAccountRouteAndQueryAsync()
    {
        await using var context = new BunitContext();
        context.AddAuthorization().SetAuthorized("coach");
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/Account/Manage/Email?notice=saved");
        var layout = context.Render<MainLayout>();
        layout.Find(".account-skip").GetAttribute("href").ShouldBe("http://localhost/Account/Manage/Email?notice=saved#account-content");
        layout.Find(".account-skip").GetAttribute("data-enhance-nav").ShouldBe("false");
        layout.Find("#account-content").GetAttribute("tabindex").ShouldBe("-1");
    }
}
