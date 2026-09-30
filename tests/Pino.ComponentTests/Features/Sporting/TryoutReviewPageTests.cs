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
public sealed class TryoutReviewPageTests
{
    private static TryoutReview Data(bool closed = false, bool archived = false, bool complete = true)
    {
        var season = new SeasonSummary(Guid.NewGuid(), "Spring", new(2027, 1, 1), new(2027, 6, 30), archived, 1);
        var player = new TryoutResult(Guid.NewGuid(), "Jordan", "Rivera", 2030, "17", complete ? DecisionKind.Placed : DecisionKind.Awaiting, complete ? Guid.NewGuid() : null, complete ? "Blue" : null);
        var history = new TryoutCloseoutSummary(Guid.NewGuid(), "Original tryout", "Original season", new(2027, 2, 20), DateTimeOffset.Parse("2027-02-21T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture), "Avery Coach", null, null, null, [player with { FirstName = "Recorded name", Bib = "12" }]);
        return new(new(Guid.NewGuid(), season.Id, "Evaluation", new(2027, 2, 20), "Field 3", 1, complete ? 1 : 0, 5, closed), season, [player], closed ? [history] : [], "review-token");
    }

    private static (ISportGateway Gateway, IRenderedComponent<TryoutReviewPage> Page) Render(BunitContext context, TryoutReview data, bool interactive = true)
    {
        context.AddBunitPersistentComponentState();
        context.AddAuthorization().SetAuthorized("member");
        var club = Guid.NewGuid();
        var clubs = Substitute.For<IClubGateway>();
        clubs.GetAccessAsync(Arg.Any<CancellationToken>()).Returns(new AccessSnapshot(new("Avery", "Coach", new("/photo", UriKind.Relative)), new(new(club, "Northside", "Soccer", "Chicago", "IL"), ClubRole.Coach), null));
        var gateway = Substitute.For<ISportGateway>();
        gateway.GetTryoutReviewAsync(club, data.Tryout.Id, Arg.Any<CancellationToken>()).Returns(data);
        context.Services.AddSingleton(clubs);
        context.Services.AddSingleton(gateway);
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        context.SetRendererInfo(new("Server", interactive));
        return (gateway, context.Render<TryoutReviewPage>(parameters => parameters.Add(page => page.ClubId, club).Add(page => page.TryoutId, data.Tryout.Id)));
    }

    [Fact]
    public async Task ClosingRequiresConfirmationAndUsesReviewedTokenAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var (gateway, page) = Render(context, data);
        gateway.CloseTryoutAsync(Arg.Any<Guid>(), data.Tryout.Id, Arg.Any<CloseTryoutInput>(), Arg.Any<CancellationToken>()).Returns(new SportReply(SportReplyKind.Conflict, "Review changed; reload."));
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Continue to confirmation", StringComparison.Ordinal)).ClickAsync();
        page.Find(".closeout-action button").HasAttribute("disabled").ShouldBeTrue();
        await page.Find(".closeout-action").SubmitAsync();
        await gateway.DidNotReceiveWithAnyArgs().CloseTryoutAsync(Guid.Empty, Guid.Empty, default!, default);
        await page.Find("input[type='checkbox']").ChangeAsync(true);
        await page.Find(".closeout-action").SubmitAsync();
        await gateway.Received(1).CloseTryoutAsync(Arg.Any<Guid>(), data.Tryout.Id, Arg.Is<CloseTryoutInput>(input => input.ReviewToken == "review-token" && input.OperationId != Guid.Empty), Arg.Any<CancellationToken>());
        page.Find(".notice[data-kind='error']").TextContent.ShouldContain("Review changed");
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    public async Task ArchiveIncompleteOrPrerenderBlocksCloseAsync(bool archived, bool complete, bool interactive)
    {
        await using var context = new BunitContext();
        var (_, page) = Render(context, Data(archived: archived, complete: complete), interactive);
        page.FindAll(".closeout-action").ShouldBeEmpty();
        page.FindAll("button").Single(value => string.Equals(value.TextContent, "Continue to confirmation", StringComparison.Ordinal)).HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task ClosedEditionShowsSnapshotAndReopeningPreservesReasonOnFailureAsync()
    {
        await using var context = new BunitContext();
        var data = Data(closed: true);
        var (gateway, page) = Render(context, data);
        gateway.ReopenTryoutAsync(Arg.Any<Guid>(), data.Tryout.Id, Arg.Any<ReopenTryoutInput>(), Arg.Any<CancellationToken>()).Returns(new SportReply(SportReplyKind.Invalid, "Season was archived."));
        page.Find(".closeout-receipt").TextContent.ShouldContain("Original tryout");
        page.Find(".result-ledger").TextContent.ShouldContain("Recorded name Rivera");
        page.Find(".result-ledger").TextContent.ShouldNotContain("Jordan");
        await page.Find("#reopen-reason").ChangeAsync("Correct a placement");
        await page.Find(".closeout-action form").SubmitAsync();
        await gateway.Received(1).ReopenTryoutAsync(Arg.Any<Guid>(), data.Tryout.Id, Arg.Is<ReopenTryoutInput>(input => input.CloseoutId == data.Closeouts[0].Id && input.Reason == "Correct a placement"), Arg.Any<CancellationToken>());
        page.Find("#reopen-reason").GetAttribute("value").ShouldBe("Correct a placement");
        await page.Find("#closeout-edition").ChangeAsync("");
        page.Find(".result-ledger").TextContent.ShouldContain("Jordan Rivera");
        page.FindAll("form.closeout-action").ShouldBeEmpty();
    }

    [Fact]
    public async Task ResultSearchUsesShownEditionAndOutcomeAsync()
    {
        await using var context = new BunitContext();
        var (_, page) = Render(context, Data(closed: true));
        await page.Find("#result-search").InputAsync("12");
        page.FindAll(".result-ledger li").Count.ShouldBe(1);
        await page.Find("#result-decision").ChangeAsync("NotSelected");
        page.FindAll(".result-ledger li").ShouldBeEmpty();
        page.Markup.ShouldContain("No results match these filters.");
    }

    [Fact]
    public async Task UncertainCloseRetriesTheSameOperationAndDisplaysTheSavedEditionAsync()
    {
        await using var context = new BunitContext();
        var data = Data();
        var (gateway, page) = Render(context, data);
        var operations = new List<Guid>();
        gateway.CloseTryoutAsync(Arg.Any<Guid>(), data.Tryout.Id, Arg.Any<CloseTryoutInput>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                operations.Add(call.Arg<CloseTryoutInput>().OperationId);
                return operations.Count == 1
                    ? Task.FromException<SportReply>(new HttpRequestException("Lost acknowledgement"))
                    : Task.FromResult(new SportReply(SportReplyKind.Saved, "Results recorded."));
            });
        var edition = Data(closed: true).Closeouts[0];
        gateway.GetTryoutReviewAsync(Arg.Any<Guid>(), data.Tryout.Id, Arg.Any<CancellationToken>())
            .Returns(data with { Tryout = data.Tryout with { Closed = true }, Closeouts = [edition] });
        await page.FindAll("button").Single(value => string.Equals(value.TextContent, "Continue to confirmation", StringComparison.Ordinal)).ClickAsync();
        await page.Find("input[type='checkbox']").ChangeAsync(true);
        await page.Find("form.closeout-action").SubmitAsync();
        page.Find(".notice[data-kind='error']").TextContent.ShouldNotBeNullOrWhiteSpace();
        page.Find("input[type='checkbox']").HasAttribute("checked").ShouldBeTrue();
        await page.Find("form.closeout-action").SubmitAsync();
        operations.Count.ShouldBe(2);
        operations[0].ShouldNotBe(Guid.Empty);
        operations[1].ShouldBe(operations[0]);
        page.Find(".closeout-receipt").TextContent.ShouldContain("Results recorded");
        page.FindAll("form.closeout-action").ShouldBeEmpty();
    }
}
