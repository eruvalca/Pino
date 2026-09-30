using System.Diagnostics.CodeAnalysis;
using Bogus;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Pino.UI.Layout;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Layout;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal",
    Justification = "xUnit requires public test classes for discovery.")]
public sealed class NavMenuTests
{
    [Theory]
    [InlineData("", true)]
    [InlineData("club/access?editProfile=true", true)]
    [InlineData("clubs/123/players/new", true)]
    [InlineData("clubs/123/tryouts/456#notes", true)]
    [InlineData("Account/Manage", false)]
    [InlineData("Account/Manage/Email", false)]
    [InlineData("Account/Login?ReturnUrl=/clubs/123", false)]
    public async Task WorkspaceSelectionTracksTheCurrentModeAsync(string route, bool selected)
    {
        await using var context = new BunitContext();
        context.AddAuthorization().SetAuthorized("member");
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("Account/Manage");
        var menu = context.Render<NavMenu>();
        await menu.InvokeAsync(() => navigation.NavigateTo(route));
        menu.Find(".navigation-links a").HasAttribute("aria-current").ShouldBe(selected);
        menu.Find(".navigation-links a").ClassList.Contains("active").ShouldBe(selected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AuthenticationStateSelectsGuestLinksOrAccountAndLogoutAsync(bool authenticated)
    {
        await using var context = new BunitContext();
        var authorization = context.AddAuthorization();
        var faker = new Faker { Random = new Randomizer(7214) };
        var userName = faker.Internet.UserName() + "<admin>";
        if (authenticated)
        {
            authorization.SetAuthorized(userName);
        }

        var component = context.Render<NavMenu>();

        if (authenticated)
        {
            component.Find("a[href='Account/Manage']").TextContent.ShouldBe("Account");
            component.FindAll("admin, a[href='Account/Login'], a[href='Account/Register']").ShouldBeEmpty();
            component.Find("form[action='Account/Logout']").GetAttribute("method").ShouldBe("post");
        }
        else
        {
            component.Find("a[href='Account/Login']").TextContent.ShouldBe("Login");
            component.Find("a[href='Account/Register']").TextContent.ShouldBe("Register");
            component.FindAll("form[action='Account/Logout'], a[href='Account/Manage']").ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task LogoutReturnUrlTracksNavigationAndStopsUpdatingAfterDisposalAsync()
    {
        await using var context = new BunitContext();
        context.AddAuthorization().SetAuthorized("member");
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("events?search=a%26b#upcoming");
        var component = context.Render<NavMenu>();
        component.Find("input[name='ReturnUrl']").GetAttribute("value").ShouldBe("events?search=a%26b#upcoming");

        await component.InvokeAsync(() => navigation.NavigateTo("counter?count=2&mode=short#value"));

        component.Find("input[name='ReturnUrl']").GetAttribute("value").ShouldBe("counter?count=2&mode=short#value");
        await component.InvokeAsync(component.Instance.Dispose);

        await component.InvokeAsync(() => navigation.NavigateTo("auth"));

        component.Find("input[name='ReturnUrl']").GetAttribute("value").ShouldBe("counter?count=2&mode=short#value");
    }
}
