using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Pages;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Sporting;

public sealed partial class TryoutWorkTests
{
    [Fact]
    public async Task ImportStepsKeepMappingAndRetryTheSameUnconfirmedCommitAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context, Data(), ClubRole.Administrator);
        var preview = new ImportReport([new(2, "", "Jordan Rivera", 2030, null)], false, "Ready to import.",
            ["First", "Last", "Graduation"], new(FirstName: 0, LastName: 1, GraduationYear: 2), new(0, 0, 0, 0, 0));
        var commits = new List<ImportInput>();
        gateway.ImportAsync(_clubId, Arg.Any<ImportInput>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var input = call.ArgAt<ImportInput>(1);
            if (!input.Commit) { return Task.FromResult(preview); }
            commits.Add(input);
            return commits.Count == 1 ? Task.FromException<ImportReport>(new HttpRequestException("Lost response")) :
                Task.FromResult(preview with { Saved = true, Message = "Imported one player.", Counts = new(1, 0, 0, 0, 0) });
        });
        var page = context.Render<PlayerImport>(parameters => parameters.Add(value => value.ClubId, _clubId));
        page.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("First,Last,Graduation\nJordan,Rivera,2030", "players.csv"));
        await page.WaitForAssertionAsync(() => page.Markup.ShouldContain("players.csv"));
        page.FindAll("#map-first, .import-review-list").ShouldBeEmpty();
        await ClickWorkflowButtonAsync(page, "Continue to columns");
        page.FindAll("#import-file, .import-review-list").ShouldBeEmpty();
        await ClickWorkflowButtonAsync(page, "Review players");
        page.FindAll("#import-file, #map-first").ShouldBeEmpty();
        await ClickWorkflowButtonAsync(page, "Back to columns");
        page.Find("#map-year").GetAttribute("value").ShouldBe("2");
        await ClickWorkflowButtonAsync(page, "Review players");
        commits.ShouldBeEmpty();
        await ClickWorkflowButtonAsync(page, "Import reviewed players");
        page.FindAll("button").Single(value => string.Equals(value.TextContent, "Back to columns", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeTrue();
        page.Markup.ShouldContain("response was interrupted");
        page.Find(".import-review-list select").HasAttribute("disabled").ShouldBeTrue();
        await ClickWorkflowButtonAsync(page, "Import reviewed players");
        commits.Count.ShouldBe(2);
        commits[0].OperationId.ShouldNotBe(Guid.Empty);
        commits[1].ShouldBe(commits[0]);
        page.Find(".workflow-progress [aria-current='step']").TextContent.ShouldContain("Import result");
        page.FindAll("button").ShouldNotContain(value => string.Equals(value.TextContent, "Import reviewed players", StringComparison.Ordinal));
    }

    [Fact]
    public async Task NewTryoutReviewsCurrentRosterBeforeSavingAndBackKeepsDetailsAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        gateway.GetOverviewAsync(_clubId, Arg.Any<CancellationToken>()).Returns(new SportOverview([data.Season], data.Teams, [], 200));
        gateway.GetTeamAvailabilityAsync(_clubId, data.Season.Id, null, Arg.Any<CancellationToken>()).Returns(new TeamAvailabilitySummary[] { new(data.Teams[0], false, 0, false, 0) });
        Guid? created = null;
        gateway.SaveTryoutAsync(_clubId, Arg.Any<TryoutInput>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            created = call.ArgAt<TryoutInput>(1).Id;
            return new SportReply(SportReplyKind.Saved, "Tryout saved.");
        });
        var page = context.Render<Seasons>(parameters => parameters.Add(value => value.ClubId, _clubId).Add(value => value.SeasonId, data.Season.Id));
        await ClickWorkflowButtonAsync(page, "Add tryout");
        await page.Find("#tryout-name").Closest("form")!.SubmitAsync();
        page.FindAll("section[aria-label='Tryout setup review']").ShouldBeEmpty();
        await page.Find("#tryout-name").ChangeAsync("Spring evaluation");
        await page.Find("#tryout-location").ChangeAsync("Field 2");
        await page.Find("#tryout-name").Closest("form")!.SubmitAsync();
        page.Find("section[aria-label='Tryout setup review']").TextContent.ShouldContain("200 active players");
        await gateway.DidNotReceive().SaveTryoutAsync(_clubId, Arg.Any<TryoutInput>(), Arg.Any<CancellationToken>());
        await ClickWorkflowButtonAsync(page, "Back to details");
        page.Find("#tryout-location").GetAttribute("value").ShouldBe("Field 2");
        await page.Find("#tryout-name").Closest("form")!.SubmitAsync();
        await ClickWorkflowButtonAsync(page, "Create & open tryout");
        await gateway.Received(1).SaveTryoutAsync(_clubId, Arg.Is<TryoutInput>(value => value.Name == "Spring evaluation" && value.Location == "Field 2" && value.SeasonId == data.Season.Id), Arg.Any<CancellationToken>());
        created.ShouldNotBeNull();
        context.Services.GetRequiredService<NavigationManager>().Uri.ShouldEndWith($"/clubs/{_clubId}/tryouts/{created}");
    }

    private static Task ClickWorkflowButtonAsync<T>(IRenderedComponent<T> page, string label) where T : IComponent =>
        page.FindAll("button").Single(value => string.Equals(value.TextContent, label, StringComparison.Ordinal)).ClickAsync();
}
