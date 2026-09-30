using System.Diagnostics.CodeAnalysis;
using Bunit;
using Cropper.Blazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Pino.SharedKernel.Clubs;
using Pino.UI.Features.Clubs.Pages;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Clubs;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "xUnit requires public test classes for discovery.")]
[SuppressMessage("Usage", "xUnit1051:Use TestContext.Current.CancellationToken", Justification = "These calls configure and verify substitutes; rendered components supply their operation tokens.")]
public sealed class ClubAccessTests
{
    private static readonly ClubSummary _club = new(new Guid(0x28d99d43, 0x9cfb, 0x42c8, 0xa3, 0x9e, 0x20, 0x85, 0xc0, 0x1, 0xd2, 0x50) /* 28d99d43-9cfb-42c8-a39e-2085c001d250 */, "Northside FC", "Soccer", "Chicago", "IL");
    private static readonly ProfileSummary _profile = new("Ana", "Diaz", new("/api/clubs/photos/ana", UriKind.Relative));

    private static IClubGateway Configure(BunitContext context, AccessSnapshot snapshot)
    {
        var gateway = Substitute.For<IClubGateway>();
        gateway.GetAccessAsync(Arg.Any<CancellationToken>()).Returns(snapshot);
        context.Services.AddSingleton(gateway);
        context.Services.AddSingleton(Substitute.For<IUrlImageInterop>());
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("/club/access");
        context.SetRendererInfo(new("Server", isInteractive: true));
        return gateway;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProfileInputsWaitForInteractiveRendererAsync(bool interactive)
    {
        await using var context = new BunitContext();
        Configure(context, new(new("", "", PhotoUrl: null), Membership: null, Request: null));
        context.SetRendererInfo(new("Server", interactive));
        var page = context.Render<ClubAccess>();
        page.Find("fieldset").HasAttribute("disabled").ShouldBe(!interactive);
    }

    [Fact]
    public async Task IncompleteProfileRequiresPhotoAndDoesNotOfferClubChoiceAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context, new(new("", "", PhotoUrl: null), Membership: null, Request: null));
        var page = context.Render<ClubAccess>();
        page.FindAll("#club-search").ShouldBeEmpty();
        await page.Find("#first-name").ChangeAsync("Ana");
        await page.Find("#last-name").ChangeAsync("Diaz");
        await page.Find("form").SubmitAsync();
        page.Find(".notice").TextContent.ShouldContain("photo");
        await gateway.DidNotReceiveWithAnyArgs().SaveProfileAsync(default!, default);
    }

    [Fact]
    public async Task SearchRequestsSelectedClubAndShowsPendingStateAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context, new(_profile, Membership: null, Request: null));
        gateway.SearchAsync("North", 0, Arg.Any<CancellationToken>()).Returns(new ClubSearchPage([_club], 0, HasMore: false));
        gateway.RequestAsync(_club.Id, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            gateway.GetAccessAsync(Arg.Any<CancellationToken>()).Returns(new AccessSnapshot(_profile, Membership: null, new(Guid.NewGuid(), _club, JoinRequestStatus.Pending, DateTimeOffset.UtcNow)));
            return new ClubReply(ClubReplyKind.Saved, "Request sent.");
        });
        var page = context.Render<ClubAccess>();
        await page.Find("#club-search").ChangeAsync("North");
        await page.Find(".search-form").SubmitAsync();
        page.Find(".club-results").TextContent.ShouldContain("Chicago, Illinois");
        await page.Find(".club-results button").ClickAsync();
        await page.WaitForAssertionAsync(() => page.Find("#task-heading").TextContent.ShouldBe("Waiting for approval"));
        await gateway.Received(1).RequestAsync(_club.Id, Arg.Any<CancellationToken>());
        page.FindAll(".club-results").ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateUsesStateSelectorAndRetainsFieldsAfterFailureAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context, new(_profile, Membership: null, Request: null));
        gateway.CreateAsync(Arg.Any<CreateClubInput>(), Arg.Any<CancellationToken>()).Returns(new ClubReply(ClubReplyKind.Unavailable, "Try again."));
        var page = context.Render<ClubAccess>();
        await page.Find(".create-alternative button").ClickAsync();
        page.FindAll("#club-state option").Count.ShouldBe(51);
        await page.Find("#club-name").ChangeAsync("Northside FC");
        await page.Find("#club-sport").ChangeAsync("Soccer");
        await page.Find("#club-city").ChangeAsync("Chicago");
        await page.Find("#club-state").ChangeAsync("IL");
        await page.Find("form").SubmitAsync();
        await gateway.Received(1).CreateAsync(Arg.Is<CreateClubInput>(value => value.State == "IL" && value.City == "Chicago" && value.OperationId != Guid.Empty), Arg.Any<CancellationToken>());
        page.Find("#club-name").GetAttribute("value").ShouldBe("Northside FC");
        page.Find(".notice").TextContent.ShouldBe("Try again.");
    }

    [Fact]
    public async Task PendingRequestCanBeCancelledAsync()
    {
        await using var context = new BunitContext();
        var requestId = Guid.NewGuid();
        var gateway = Configure(context, new(_profile, Membership: null, new(requestId, _club, JoinRequestStatus.Pending, DateTimeOffset.UtcNow)));
        gateway.CancelAsync(requestId, Arg.Any<CancellationToken>()).Returns(_ => { gateway.GetAccessAsync(Arg.Any<CancellationToken>()).Returns(new AccessSnapshot(_profile, Membership: null, Request: null)); return new ClubReply(ClubReplyKind.Saved, "Cancelled."); });
        var page = context.Render<ClubAccess>();
        await page.FindAll("button").Single(button => string.Equals(button.TextContent, "Cancel request", StringComparison.Ordinal)).ClickAsync();
        await page.WaitForAssertionAsync(() => page.Find("#task-heading").TextContent.ShouldBe("Find your club"));
        await gateway.Received(1).CancelAsync(requestId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeavingRequiresConfirmationAndDisplaysLastAdministratorConflictAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context, new(_profile, new(_club, ClubRole.Administrator), Request: null));
        gateway.LeaveAsync(_club.Id, Arg.Any<CancellationToken>()).Returns(new ClubReply(ClubReplyKind.Conflict, "Promote another member first."));
        var page = context.Render<ClubAccess>();
        await page.Find(".leave-action").ClickAsync();
        await gateway.DidNotReceiveWithAnyArgs().LeaveAsync(Guid.Empty, default);
        await page.Find(".access-confirm .primary-action").ClickAsync();
        await gateway.Received(1).LeaveAsync(_club.Id, Arg.Any<CancellationToken>());
        page.Find(".notice").TextContent.ShouldContain("Promote another");
        page.Find("#task-heading").TextContent.ShouldBe(_club.Name);
    }
}
