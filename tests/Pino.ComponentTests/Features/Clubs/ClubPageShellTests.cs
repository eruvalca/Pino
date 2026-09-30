using System.Diagnostics.CodeAnalysis;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Pino.SharedKernel.Clubs;
using Pino.UI.Features.Clubs.Components;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Clubs;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
public sealed class ClubPageShellTests
{
    [Theory]
    [InlineData("seasons", true)]
    [InlineData("seasons/123/review", true)]
    [InlineData("teams/123", true)]
    [InlineData("tryouts/123/review", true)]
    [InlineData("players/123", false)]
    public async Task SeasonSectionIncludesTeamsAndTryoutChildrenAsync(string path, bool selected)
    {
        await using var context = new BunitContext();
        context.AddAuthorization().SetAuthorized("coach");
        var club = new ClubSummary(Guid.NewGuid(), "Northside FC", "Soccer", "Chicago", "IL");
        context.Services.GetRequiredService<NavigationManager>().NavigateTo($"clubs/{club.Id}/{path}");
        var shell = context.Render<ClubPageShell>(parameters => parameters.Add(value => value.Club, club).AddChildContent("<h1>Workspace</h1>"));
        shell.Find($"a[href='/clubs/{club.Id}/seasons']").HasAttribute("aria-current").ShouldBe(selected);
        shell.FindAll($"a[href='/clubs/{club.Id}/people']").ShouldBeEmpty();
        shell.Find(".active-club").GetAttribute("href").ShouldBe("/club/access");
        shell.Find(".navigation-links form").GetAttribute("method").ShouldBe("post");
        shell.Render(parameters => parameters.Add(value => value.CanManagePeople, true));
        shell.Find($"a[href='/clubs/{club.Id}/people']").TextContent.ShouldBe("People");
    }
}
