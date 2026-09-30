using System.Diagnostics.CodeAnalysis;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Pino.Features.Account.Pages;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Account;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "xUnit requires public test classes for discovery.")]
public sealed class RegisterConfirmationTests
{
    [Fact]
    public async Task MissingEmailRedirectsHomeWithoutLookingUpUserAsync()
    {
        await using var context = new BunitContext();
        var account = context.ConfigureAccount();
        var navigation = context.Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("Account/RegisterConfirmation");
        account.Render<RegisterConfirmation>(context);
        navigation.Uri.ShouldBe("http://localhost/");
        await account.Users.DidNotReceiveWithAnyArgs().FindByEmailAsync(default!);
    }

    [Theory]
    [InlineData("private%40example.test")]
    [InlineData("member%40example.test&returnUrl=%2Fevents")]
    public async Task ConfirmationInstructionsNeverLookUpOrExposeAccountTokensAsync(string query)
    {
        await using var context = new BunitContext();
        var account = context.ConfigureAccount();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("Account/RegisterConfirmation?email=" + query);
        var component = account.Render<RegisterConfirmation>(context);
        component.Find("p[role='status']").TextContent.ShouldBe("Please check your email to confirm your account.");
        component.FindAll("a[href*='ConfirmEmail?']").ShouldBeEmpty();
        component.Markup.ShouldNotContain("@example.test");
        component.Find("a[href='Account/ResendEmailConfirmation']").TextContent.ShouldBe("request another confirmation email");
        account.Http.Response.StatusCode.ShouldBe(200);
        await account.Users.DidNotReceiveWithAnyArgs().FindByEmailAsync(default!);
        await account.Users.DidNotReceiveWithAnyArgs().GenerateEmailConfirmationTokenAsync(default!);
    }
}
