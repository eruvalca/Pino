using System.Diagnostics.CodeAnalysis;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Pino.SharedKernel.Clubs;
using Pino.UI.Features.Clubs.Pages;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Clubs;

[SuppressMessage("Maintainability", "CA1515:Consider making public types internal", Justification = "xUnit requires public test classes for discovery.")]
[SuppressMessage("Usage", "xUnit1051:Use TestContext.Current.CancellationToken", Justification = "These calls configure and verify substitutes; rendered components supply their operation tokens.")]
public sealed class ClubPeopleTests
{
    private static readonly ClubSummary _club = new(new Guid(0x28d99d43, 0x9cfb, 0x42c8, 0xa3, 0x9e, 0x20, 0x85, 0xc0, 0x1, 0xd2, 0x50) /* 28d99d43-9cfb-42c8-a39e-2085c001d250 */, "Northside FC", "Soccer", "Chicago", "IL");
    private static readonly PersonSummary _person = new("ana", "Ana", "Diaz", new("/photo/ana", UriKind.Relative), ClubRole.Coach, new Guid(0xacc75c1b, 0x204e, 0x42ed, 0xb8, 0x75, 0xea, 0x4d, 0x25, 0xf7, 0xc2, 0xc1) /* acc75c1b-204e-42ed-b875-ea4d25f7c2c1 */, DateTimeOffset.UtcNow);

    private static IClubGateway Configure(BunitContext context)
    {
        context.AddAuthorization().SetAuthorized("member");
        var gateway = Substitute.For<IClubGateway>();
        gateway.GetPeopleAsync(_club.Id, Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new PeoplePage(_club, [_person], 0, HasMore: false));
        gateway.DecideAsync(_club.Id, Arg.Any<RequestDecisionInput>(), Arg.Any<CancellationToken>()).Returns(new ClubReply(ClubReplyKind.Saved, "Handled."));
        gateway.ChangeMemberAsync(_club.Id, Arg.Any<MemberChangeInput>(), Arg.Any<CancellationToken>()).Returns(new ClubReply(ClubReplyKind.Saved, "Changed."));
        context.Services.AddSingleton(gateway);
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        context.SetRendererInfo(new("Server", isInteractive: true));
        return gateway;
    }

    [Fact]
    public async Task ApprovalSendsTheSelectedRequestAndRefreshesAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context);
        var page = context.Render<ClubPeople>(parameters => parameters.Add(value => value.ClubId, _club.Id));
        await page.Find(".person-actions .primary-action").ClickAsync();
        await gateway.Received(1).DecideAsync(_club.Id, new(_person.RequestId!.Value, Approve: true), Arg.Any<CancellationToken>());
        await gateway.Received(2).GetPeopleAsync(_club.Id, requests: true, 0, Arg.Any<CancellationToken>());
        page.Find(".notice").TextContent.ShouldBe("Handled.");
    }

    [Fact]
    public async Task DenialRequiresConfirmationNamingPersonAndClubAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context);
        var page = context.Render<ClubPeople>(parameters => parameters.Add(value => value.ClubId, _club.Id));
        await page.Find(".person-actions .text-action").ClickAsync();
        page.Find(".people-confirm").TextContent.ShouldContain("Ana Diaz");
        page.Find(".people-confirm").TextContent.ShouldContain("Northside FC");
        await gateway.DidNotReceiveWithAnyArgs().DecideAsync(Guid.Empty, default!, default);
        await page.Find(".people-confirm .primary-action").ClickAsync();
        await gateway.Received(1).DecideAsync(_club.Id, new(_person.RequestId!.Value, Approve: false), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RoleChangeAndRemovalSendExpectedRoleAfterConfirmationAsync(bool promote)
    {
        await using var context = new BunitContext();
        var gateway = Configure(context);
        var page = context.Render<ClubPeople>(parameters => parameters.Add(value => value.ClubId, _club.Id));
        await page.FindAll(".people-views button")[1].ClickAsync();
        await page.Find(promote ? ".person-actions .secondary-action" : ".person-actions .text-action").ClickAsync();
        await gateway.DidNotReceiveWithAnyArgs().ChangeMemberAsync(Guid.Empty, default!, default);
        page.Find(".people-confirm").TextContent.ShouldContain("Ana Diaz");
        await page.Find(".people-confirm .primary-action").ClickAsync();
        await gateway.Received(1).ChangeMemberAsync(_club.Id, new(_person.UserId, ClubRole.Coach, promote ? ClubRole.Administrator : null), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokedAdministratorAccessClearsPeopleAndConfirmationAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context);
        var page = context.Render<ClubPeople>(parameters => parameters.Add(value => value.ClubId, _club.Id));
        await page.Find(".person-actions .text-action").ClickAsync();
        page.Find(".active-club").TextContent.ShouldBe(_club.Name);
        gateway.GetPeopleAsync(_club.Id, requests: true, 0, Arg.Any<CancellationToken>()).Returns(Task.FromException<PeoplePage>(new UnauthorizedAccessException()));
        await page.FindAll(".people-views button")[2].ClickAsync();
        page.FindAll(".people-list, .people-confirm").ShouldBeEmpty();
        page.FindAll(".active-club").ShouldBeEmpty();
        page.Markup.ShouldNotContain("Ana Diaz");
        page.Find(".notice").TextContent.ShouldContain("no longer have administrator access");
    }

    [Fact]
    public async Task SwitchingViewsKeepsRowsAndActionsTogetherUntilLoadCompletesAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context);
        gateway.GetPeopleAsync(_club.Id, requests: false, 0, Arg.Any<CancellationToken>()).Returns(new PeoplePage(_club, [_person with { RequestId = null }], 0, HasMore: false));
        var page = context.Render<ClubPeople>(parameters => parameters.Add(value => value.ClubId, _club.Id));
        await page.FindAll(".people-views button")[1].ClickAsync();
        var requests = new TaskCompletionSource<PeoplePage>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.GetPeopleAsync(_club.Id, requests: true, 0, Arg.Any<CancellationToken>()).Returns(requests.Task);

        var switching = page.FindAll(".people-views button")[0].ClickAsync();
        await page.WaitForAssertionAsync(() =>
        {
            page.Find("h2").TextContent.ShouldBe("Club members");
            page.Find("[role='status']").TextContent.ShouldBe("Loading people…");
            page.FindAll(".person-actions .primary-action").ShouldBeEmpty();
            page.FindAll("main button").ShouldAllBe(button => button.HasAttribute("disabled"));
        });
        requests.SetResult(new(_club, [_person], 0, HasMore: false));
        await switching;
        page.Find("h2").TextContent.ShouldBe("Membership requests");
        page.Find(".person-actions .primary-action").HasAttribute("disabled").ShouldBeFalse();
        await page.Find(".person-actions .primary-action").ClickAsync();
        await gateway.Received(1).DecideAsync(_club.Id, new(_person.RequestId!.Value, Approve: true), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LateLoadCannotReplaceNewClubDataAsync(bool fails)
    {
        await using var context = new BunitContext();
        var gateway = Configure(context);
        var older = new TaskCompletionSource<PeoplePage>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.GetPeopleAsync(_club.Id, requests: true, 0, Arg.Any<CancellationToken>()).Returns(older.Task);
        var other = _club with { Id = new Guid(0xed1dc61b, 0xa49a, 0x462e, 0xae, 0xfe, 0x4, 0x1b, 0xa5, 0xbb, 0x3c, 0x18) /* ed1dc61b-a49a-462e-aefe-041ba5bb3c18 */, Name = "Harbor United" };
        gateway.GetPeopleAsync(other.Id, requests: true, 0, Arg.Any<CancellationToken>()).Returns(new PeoplePage(other, [], 0, HasMore: false));
        var page = context.Render<ClubPeople>(parameters => parameters.Add(value => value.ClubId, _club.Id));
        page.Render(parameters => parameters.Add(value => value.ClubId, other.Id));
        await page.WaitForAssertionAsync(() => page.Find(".active-club").TextContent.ShouldBe(other.Name));
        var renders = page.RenderCount;

        if (fails) { older.SetException(new UnauthorizedAccessException()); }
        else { older.SetResult(new(_club, [_person], 0, HasMore: false)); }

        await page.WaitForAssertionAsync(() => page.RenderCount.ShouldBeGreaterThan(renders));
        page.Find(".active-club").TextContent.ShouldBe(other.Name);
        page.FindAll(".people-list li, .notice").ShouldBeEmpty();
        page.FindAll(".people-views button").ShouldAllBe(button => !button.HasAttribute("disabled"));
    }

    [Fact]
    public async Task OlderCompletionCannotEnableControlsWhileNewClubIsLoadingAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context);
        var older = new TaskCompletionSource<PeoplePage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var newer = new TaskCompletionSource<PeoplePage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var other = _club with { Id = new Guid(0xed1dc61b, 0xa49a, 0x462e, 0xae, 0xfe, 0x4, 0x1b, 0xa5, 0xbb, 0x3c, 0x18) /* ed1dc61b-a49a-462e-aefe-041ba5bb3c18 */, Name = "Harbor United" };
        gateway.GetPeopleAsync(_club.Id, requests: true, 0, Arg.Any<CancellationToken>()).Returns(older.Task);
        gateway.GetPeopleAsync(other.Id, requests: true, 0, Arg.Any<CancellationToken>()).Returns(newer.Task);
        var page = context.Render<ClubPeople>(parameters => parameters.Add(value => value.ClubId, _club.Id));
        page.Render(parameters => parameters.Add(value => value.ClubId, other.Id));
        var renders = page.RenderCount;
        older.SetResult(new(_club, [_person], 0, HasMore: false));

        await page.WaitForAssertionAsync(() => page.RenderCount.ShouldBeGreaterThan(renders));
        page.FindAll(".people-list li, .active-club").ShouldBeEmpty();
        page.FindAll(".people-views button").ShouldAllBe(button => button.HasAttribute("disabled"));
        newer.SetResult(new(other, [], 0, HasMore: false));
        await page.WaitForAssertionAsync(() => page.Find(".active-club").TextContent.ShouldBe(other.Name));
        page.FindAll(".people-views button").ShouldAllBe(button => !button.HasAttribute("disabled"));
    }
}
