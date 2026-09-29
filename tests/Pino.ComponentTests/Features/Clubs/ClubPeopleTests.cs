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
    private static readonly ClubSummary _club = new(Guid.Parse("28d99d43-9cfb-42c8-a39e-2085c001d250"), "Northside FC", "Soccer", "Chicago", "IL");
    private static readonly PersonSummary _person = new("ana", "Ana", "Diaz", new("/photo/ana", UriKind.Relative), ClubRole.Coach, Guid.Parse("acc75c1b-204e-42ed-b875-ea4d25f7c2c1"), DateTimeOffset.UtcNow);

    private static IClubGateway Configure(BunitContext context)
    {
        var gateway = Substitute.For<IClubGateway>();
        gateway.GetPeopleAsync(_club.Id, Arg.Any<bool>(), Arg.Any<int>()).Returns(new PeoplePage(_club, [_person], 0, false));
        gateway.DecideAsync(_club.Id, Arg.Any<RequestDecisionInput>()).Returns(new ClubReply(ClubReplyKind.Saved, "Handled."));
        gateway.ChangeMemberAsync(_club.Id, Arg.Any<MemberChangeInput>()).Returns(new ClubReply(ClubReplyKind.Saved, "Changed."));
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
        await gateway.Received(1).DecideAsync(_club.Id, new(_person.RequestId!.Value, true));
        await gateway.Received(2).GetPeopleAsync(_club.Id, true, 0);
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
        await gateway.Received(1).DecideAsync(_club.Id, new(_person.RequestId!.Value, false));
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
        await gateway.Received(1).ChangeMemberAsync(_club.Id, new(_person.UserId, ClubRole.Coach, promote ? ClubRole.Administrator : null));
    }

    [Fact]
    public async Task RevokedAdministratorAccessClearsPeopleAndConfirmationAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context);
        var page = context.Render<ClubPeople>(parameters => parameters.Add(value => value.ClubId, _club.Id));
        await page.Find(".person-actions .text-action").ClickAsync();
        page.Find(".active-club").TextContent.ShouldBe(_club.Name);
        gateway.GetPeopleAsync(_club.Id, true, 0).Returns(Task.FromException<PeoplePage>(new UnauthorizedAccessException()));
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
        gateway.GetPeopleAsync(_club.Id, false, 0).Returns(new PeoplePage(_club, [_person with { RequestId = null }], 0, false));
        var page = context.Render<ClubPeople>(parameters => parameters.Add(value => value.ClubId, _club.Id));
        await page.FindAll(".people-views button")[1].ClickAsync();
        var requests = new TaskCompletionSource<PeoplePage>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.GetPeopleAsync(_club.Id, true, 0).Returns(requests.Task);

        var switching = page.FindAll(".people-views button")[0].ClickAsync();
        await page.WaitForAssertionAsync(() =>
        {
            page.Find("h2").TextContent.ShouldBe("Club members");
            page.Find("[role='status']").TextContent.ShouldBe("Loading people…");
            page.FindAll(".person-actions .primary-action").ShouldBeEmpty();
            page.FindAll("button").ShouldAllBe(button => button.HasAttribute("disabled"));
        });
        requests.SetResult(new(_club, [_person], 0, false));
        await switching;
        page.Find("h2").TextContent.ShouldBe("Membership requests");
        page.Find(".person-actions .primary-action").HasAttribute("disabled").ShouldBeFalse();
        await page.Find(".person-actions .primary-action").ClickAsync();
        await gateway.Received(1).DecideAsync(_club.Id, new(_person.RequestId!.Value, true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LateLoadCannotReplaceNewClubDataAsync(bool fails)
    {
        await using var context = new BunitContext();
        var gateway = Configure(context);
        var older = new TaskCompletionSource<PeoplePage>(TaskCreationOptions.RunContinuationsAsynchronously);
        gateway.GetPeopleAsync(_club.Id, true, 0).Returns(older.Task);
        var other = _club with { Id = Guid.Parse("ed1dc61b-a49a-462e-aefe-041ba5bb3c18"), Name = "Harbor United" };
        gateway.GetPeopleAsync(other.Id, true, 0).Returns(new PeoplePage(other, [], 0, false));
        var page = context.Render<ClubPeople>(parameters => parameters.Add(value => value.ClubId, _club.Id));
        page.Render(parameters => parameters.Add(value => value.ClubId, other.Id));
        await page.WaitForAssertionAsync(() => page.Find(".active-club").TextContent.ShouldBe(other.Name));
        var renders = page.RenderCount;

        if (fails) { older.SetException(new UnauthorizedAccessException()); }
        else { older.SetResult(new(_club, [_person], 0, false)); }

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
        var other = _club with { Id = Guid.Parse("ed1dc61b-a49a-462e-aefe-041ba5bb3c18"), Name = "Harbor United" };
        gateway.GetPeopleAsync(_club.Id, true, 0).Returns(older.Task);
        gateway.GetPeopleAsync(other.Id, true, 0).Returns(newer.Task);
        var page = context.Render<ClubPeople>(parameters => parameters.Add(value => value.ClubId, _club.Id));
        page.Render(parameters => parameters.Add(value => value.ClubId, other.Id));
        var renders = page.RenderCount;
        older.SetResult(new(_club, [_person], 0, false));

        await page.WaitForAssertionAsync(() => page.RenderCount.ShouldBeGreaterThan(renders));
        page.FindAll(".people-list li, .active-club").ShouldBeEmpty();
        page.FindAll(".people-views button").ShouldAllBe(button => button.HasAttribute("disabled"));
        newer.SetResult(new(other, [], 0, false));
        await page.WaitForAssertionAsync(() => page.Find(".active-club").TextContent.ShouldBe(other.Name));
        page.FindAll(".people-views button").ShouldAllBe(button => !button.HasAttribute("disabled"));
    }
}
