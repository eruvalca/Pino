using System.Diagnostics.CodeAnalysis;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Sporting.Pages;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Sporting;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
[SuppressMessage("Usage", "xUnit1051:Use TestContext.Current.CancellationToken", Justification = "Substitute argument matching accepts the component-owned cancellation token.")]
public sealed partial class TryoutWorkTests
{
    private static readonly Guid _clubId = new(0xc128085d, 0xd05c, 0x42ec, 0x92, 0x53, 0x5, 0x85, 0xd2, 0x8e, 0x59, 0xb1) /* c128085d-d05c-42ec-9253-0585d28e59b1 */;
    private static readonly Guid _tryoutId = new(0x8c4c9e4b, 0xf132, 0x4c47, 0x8e, 0x7a, 0xf9, 0x92, 0x3b, 0x53, 0x62, 0x60) /* 8c4c9e4b-f132-4c47-8e7a-f9923b536260 */;
    private static TryoutDetail Data(bool archived = false)
    {
        var season = new SeasonSummary(Guid.NewGuid(), "Spring", new(2027, 1, 1), new(2027, 6, 30), archived, 1);
        var player = new PlayerSummary(Guid.NewGuid(), "NS-001", "Jordan", "Rivera", 2030, "Midfield", "", Archived: false, 1, PhotoUrl: null);
        var second = player with { Id = Guid.NewGuid(), PlayerReference = "NS-002", FirstName = "Casey", LastName = "Chen" };
        return new(new(_tryoutId, season.Id, "Spring evaluation", new(2027, 2, 20), "", 2, 0, 1), season,
            [new(Guid.NewGuid(), "Eligible 2030", 2030, Archived: false, 1), new(Guid.NewGuid(), "Ineligible 2031", 2031, Archived: false, 1)],
            [new(player, "17", DecisionKind.Awaiting, DecisionTeamId: null, 3, CurrentTeamId: null, CurrentTryoutId: null, 8), new(second, "22", DecisionKind.Awaiting, DecisionTeamId: null, 1, CurrentTeamId: null, CurrentTryoutId: null, 0)]);
    }
    private static ISportGateway Configure(BunitContext context, TryoutDetail data, ClubRole role = ClubRole.Coach)
    {
        context.AddBunitPersistentComponentState();
        context.AddAuthorization().SetAuthorized("member");
        var clubs = Substitute.For<IClubGateway>();
        clubs.GetAccessAsync(Arg.Any<CancellationToken>()).Returns(new AccessSnapshot(new("Avery", "Coach", new("/photo", UriKind.Relative)),
            new(new(_clubId, "Northside", "Soccer", "Chicago", "IL"), role), Request: null));
        var sport = Substitute.For<ISportGateway>();
        sport.GetTryoutAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>()).Returns(data);
        sport.GetOverviewAsync(_clubId, Arg.Any<CancellationToken>()).Returns(new SportOverview([data.Season], data.Teams, [data.Tryout]));
        sport.GetNotebookAsync(_clubId, _tryoutId, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(call => new PlayerNotebook(call.ArgAt<Guid>(2), false, [], [], []));
        context.Services.AddSingleton(clubs);
        context.Services.AddSingleton(sport);
        context.SetRendererInfo(new("Server", isInteractive: true));
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        return sport;
    }
    private static IRenderedComponent<TryoutWork> Render(BunitContext context) =>
        context.Render<TryoutWork>(parameters => parameters.Add(page => page.ClubId, _clubId).Add(page => page.TryoutId, _tryoutId));

    [Fact]
    public async Task SwitchingPlayersRetainsIndependentUnsavedNotesAsync()
    {
        await using var context = new BunitContext();
        Configure(context, Data());
        var page = Render(context);
        await page.Find("#player-note").InputAsync("Jordan's unsaved observation");
        await page.FindAll(".roster-record")[1].ClickAsync();
        page.Find("#player-note").GetAttribute("value").ShouldBeEmpty();
        await page.Find("#player-note").InputAsync("Casey's unsaved observation");
        await page.FindAll(".roster-record")[0].ClickAsync();
        page.Find("#player-note").GetAttribute("value").ShouldBe("Jordan's unsaved observation");
        await page.FindAll(".roster-record")[1].ClickAsync();
        page.Find("#player-note").GetAttribute("value").ShouldBe("Casey's unsaved observation");
    }

    [Fact]
    public async Task PlacementOffersOnlyEligibleTeamsAndSubmitsObservedVersionsAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        gateway.DecideAsync(_clubId, _tryoutId, Arg.Any<DecisionInput>(), Arg.Any<CancellationToken>()).Returns(new SportReply(SportReplyKind.Conflict, "This record changed. Reload."));
        var page = Render(context);
        await page.Find("#decision-kind").ChangeAsync("Placed");
        page.FindAll("#decision-team option").Select(option => option.TextContent).ShouldBe(["Choose a team", "Eligible 2030 · 2030+"]);
        await page.Find("#decision-team").ChangeAsync(data.Teams[0].Id.ToString());
        await page.Find("#decision-reason").ChangeAsync("Ready for the team.");
        await page.Find("#decision-kind").Closest("form")!.SubmitAsync();
        await gateway.Received(1).DecideAsync(_clubId, _tryoutId, Arg.Is<DecisionInput>(input =>
            input.PlayerId == data.Roster[0].Player.Id && input.TeamId == data.Teams[0].Id && input.Revision == 3 &&
            input.PlacementRevision == 8 && input.Reason == "Ready for the team." && input.OperationId != Guid.Empty), Arg.Any<CancellationToken>());
        await page.WaitForAssertionAsync(() => page.Find(".notice[data-kind='error']").TextContent.ShouldContain("record changed"));
        page.Find("#decision-reason").GetAttribute("value").ShouldBe("Ready for the team.");
    }

    [Fact]
    public async Task TransportFailureKeepsNoteForRetryAndDoesNotClaimSuccessAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context, Data());
        gateway.AddNoteAsync(_clubId, _tryoutId, Arg.Any<NoteInput>(), Arg.Any<CancellationToken>()).Returns(Task.FromException<SportReply>(new HttpRequestException("offline")));
        var page = Render(context);
        await page.Find("#player-note").InputAsync("Keep this observation.");
        await page.Find(".note-composer").SubmitAsync();
        page.Find("#player-note").GetAttribute("value").ShouldBe("Keep this observation.");
        page.Find(".notice[data-kind='error']").TextContent.ShouldContain("unsaved input is still here");
    }

    [Fact]
    public async Task SuccessfulSaveClearsOnlySavedPlayerDraftAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var gateway = Configure(context, data);
        gateway.AddNoteAsync(_clubId, _tryoutId, Arg.Any<NoteInput>(), Arg.Any<CancellationToken>()).Returns(new SportReply(SportReplyKind.Saved, "Shared note saved."));
        var page = Render(context);
        await page.Find("#player-note").InputAsync("Saved Jordan note.");
        await page.FindAll(".roster-record")[1].ClickAsync();
        await page.Find("#player-note").InputAsync("Unsaved Casey note.");
        await page.FindAll(".roster-record")[0].ClickAsync();
        await page.Find(".note-composer").SubmitAsync();
        page.Find("#player-note").GetAttribute("value").ShouldBeEmpty();
        await page.FindAll(".roster-record")[1].ClickAsync();
        page.Find("#player-note").GetAttribute("value").ShouldBe("Unsaved Casey note.");
    }

    [Fact]
    public async Task RevokedMembershipClearsProtectedContentAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context, Data());
        var page = Render(context);
        gateway.GetTryoutAsync(_clubId, _tryoutId, Arg.Any<CancellationToken>()).Returns(Task.FromException<TryoutDetail>(new UnauthorizedAccessException()));
        await page.FindAll("button").Single(button => string.Equals(button.TextContent, "Refresh saved decisions", StringComparison.Ordinal)).ClickAsync();
        page.Find("h1").TextContent.ShouldBe("Workspace unavailable");
        page.FindAll(".player-notebook").ShouldBeEmpty();
        page.Markup.ShouldNotContain("Jordan Rivera");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ArchivedSeasonDisablesMutationFormsAsync(bool archived)
    {
        await using var context = new BunitContext();
        Configure(context, Data(archived));
        var page = Render(context);
        page.FindAll(".player-notebook fieldset").Count.ShouldBe(3);
        page.FindAll(".player-notebook fieldset").All(fieldset => fieldset.HasAttribute("disabled") == archived).ShouldBeTrue();
    }

    [Fact]
    public async Task PrerenderedInputsWaitForHydrationAndCoachCannotSeePeopleLinkAsync()
    {
        await using var context = new BunitContext();
        Configure(context, Data());
        context.SetRendererInfo(new("Static", isInteractive: false));
        var page = Render(context);
        page.Find(".note-composer fieldset").HasAttribute("disabled").ShouldBeTrue();
        page.Find(".bib-editor fieldset").HasAttribute("disabled").ShouldBeTrue();
        page.FindAll($"a[href='/clubs/{_clubId}/people']").ShouldBeEmpty();
    }

    [Fact]
    public async Task FiltersShowExplicitEmptyResultWithoutDiscardingDraftAsync()
    {
        await using var context = new BunitContext();
        Configure(context, Data());
        var page = Render(context);
        await page.Find("#player-note").InputAsync("Still here.");
        await page.Find("#roster-query").InputAsync("no matching name");
        page.FindAll(".roster-record").ShouldBeEmpty();
        page.Find(".roster-no-results").TextContent.ShouldContain("Change your filters");
        page.Find("#player-note").GetAttribute("value").ShouldBe("Still here.");
    }
}
