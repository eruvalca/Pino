using System.Diagnostics.CodeAnalysis;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;
using Pino.UI.Features.Clubs.Pages;
using Shouldly;
using Xunit;

namespace Pino.ComponentTests.Features.Clubs;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "xUnit discovers public test classes.")]
[SuppressMessage("Usage", "xUnit1051:Use TestContext.Current.CancellationToken", Justification = "Substitutes match component-owned operation tokens.")]
public sealed class ClubAdministrationTests
{
    private static readonly ClubSummary _club = new(Guid.NewGuid(), "Northside", "Soccer", "Chicago", "IL", 4);
    private static readonly ProfileSummary _profile = new("Avery", "Coach", new("/photo", UriKind.Relative));

    private static IClubGateway Configure(BunitContext context)
    {
        context.AddBunitPersistentComponentState();
        context.AddAuthorization().SetAuthorized("staff");
        var gateway = Substitute.For<IClubGateway>();
        gateway.GetAccessAsync(Arg.Any<CancellationToken>()).Returns(new AccessSnapshot(_profile, new(_club, ClubRole.Administrator), null));
        gateway.GetInvitationsAsync(_club.Id, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new InvitationsPage(_club, [], 0, false));
        context.Services.AddSingleton(gateway);
        context.Services.AddSingleton(Substitute.For<ISportGateway>());
        context.SetRendererInfo(new("Server", isInteractive: true));
        context.JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true).SetVoidResult();
        return gateway;
    }

    [Fact]
    public async Task AdministratorInvitationRequiresConfirmationAndTransportRetryKeepsIdentityAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context);
        var ids = new List<Guid>();
        gateway.InviteAsync(_club.Id, Arg.Any<InvitationInput>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            var input = call.ArgAt<InvitationInput>(1);
            ids.Add(input.Id);
            input.Role.ShouldBe(ClubRole.Administrator);
            input.Email.ShouldBe("staff@example.test");
            return ids.Count == 1 ? Task.FromException<ClubReply>(new HttpRequestException("Unavailable")) :
                Task.FromResult(new ClubReply(ClubReplyKind.Saved, "Queued."));
        });
        var page = context.Render<ClubInvitations>(parameters => parameters.Add(value => value.ClubId, _club.Id));
        await page.Find("#invite-email").ChangeAsync("staff@example.test");
        await page.Find("#invite-role").ChangeAsync("Administrator");
        page.Find("main button[type='submit']").HasAttribute("disabled").ShouldBeTrue();
        await page.Find("main form").SubmitAsync();
        ids.ShouldBeEmpty();
        await page.Find("input[type='checkbox']").ChangeAsync(true);
        await page.Find("main form").SubmitAsync();
        page.Find("#invite-email").GetAttribute("value").ShouldBe("staff@example.test");
        page.Find(".notice[data-kind='error']").TextContent.ShouldContain("couldn't complete");
        await page.Find("main form").SubmitAsync();
        ids.Count.ShouldBe(2);
        ids[1].ShouldBe(ids[0]);
        page.Find("#invite-email").GetAttribute("value").ShouldBeEmpty();
        page.Find(".notice[data-kind='success']").TextContent.ShouldBe("Queued.");
    }

    [Fact]
    public async Task StaleClubDetailsPreserveEditsAndSendObservedRevisionAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context);
        gateway.SaveDetailsAsync(_club.Id, Arg.Any<ClubDetailsInput>(), Arg.Any<CancellationToken>()).Returns(new ClubReply(ClubReplyKind.Conflict, "Details changed. Reload."));
        var page = context.Render<ClubSettings>(parameters => parameters.Add(value => value.ClubId, _club.Id));
        await page.Find("#club-city").ChangeAsync("Evanston");
        await page.Find("main form").SubmitAsync();
        await gateway.Received(1).SaveDetailsAsync(_club.Id, Arg.Is<ClubDetailsInput>(value => value.Revision == 4 && value.City == "Evanston"), Arg.Any<CancellationToken>());
        page.Find("#club-city").GetAttribute("value").ShouldBe("Evanston");
        page.Find(".notice").TextContent.ShouldBe("Details changed. Reload.");
    }

    [Fact]
    public async Task InvitationOnlyGrantsAccessAfterExplicitAcceptanceAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context);
        var id = Guid.NewGuid();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo($"/club/invitations/{id}?token=secret");
        gateway.GetAccessAsync(Arg.Any<CancellationToken>()).Returns(new AccessSnapshot(_profile, null, null), new AccessSnapshot(_profile, new(_club, ClubRole.Coach), null));
        gateway.PreviewInvitationAsync(id, "secret", Arg.Any<CancellationToken>()).Returns(new InvitationPreview(_club, ClubRole.Coach, "Ready.", true), new InvitationPreview(_club, ClubRole.Coach, "Used.", false));
        gateway.AcceptInvitationAsync(id, new("secret"), Arg.Any<CancellationToken>()).Returns(new ClubReply(ClubReplyKind.Saved, "Joined as Coach."));
        var page = context.Render<AcceptInvitation>(parameters => parameters.Add(value => value.InvitationId, id));
        await gateway.DidNotReceiveWithAnyArgs().AcceptInvitationAsync(Guid.Empty, default!, default);
        await page.Find("button.primary-action").ClickAsync();
        await gateway.Received(1).AcceptInvitationAsync(id, new("secret"), Arg.Any<CancellationToken>());
        page.Find("a.primary-action").GetAttribute("href").ShouldBe($"/clubs/{_club.Id}");
        page.FindAll("button.primary-action").ShouldBeEmpty();
        page.Markup.ShouldContain("Joined as Coach.");
    }

    [Fact]
    public async Task FailedInvitationRefreshClearsPriorClubAndAcceptActionAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context);
        var id = Guid.NewGuid();
        context.Services.GetRequiredService<NavigationManager>().NavigateTo($"/club/invitations/{id}?token=secret");
        gateway.GetAccessAsync(Arg.Any<CancellationToken>()).Returns(new AccessSnapshot(_profile, null, null));
        gateway.PreviewInvitationAsync(id, "secret", Arg.Any<CancellationToken>()).Returns(new InvitationPreview(_club, ClubRole.Coach, "Ready.", true));
        var page = context.Render<AcceptInvitation>(parameters => parameters.Add(value => value.InvitationId, id));
        gateway.PreviewInvitationAsync(id, "secret", Arg.Any<CancellationToken>()).Returns(Task.FromException<InvitationPreview>(new UnauthorizedAccessException()));
        await page.Find("button.text-action").ClickAsync();
        page.Markup.ShouldNotContain("Northside");
        page.FindAll("button.primary-action").ShouldBeEmpty();
        page.Find("button.secondary-action").TextContent.ShouldBe("Try again");
    }

    [Fact]
    public async Task OnlyFailedEmailOffersRetryWithObservedRevisionAsync()
    {
        await using var context = new BunitContext();
        var gateway = Configure(context);
        var failed = new StaffEmailSummary(Guid.NewGuid(), "staff@example.test", "Invitation", StaffEmailStatus.Failed, DateTimeOffset.UtcNow, null, 5, 7);
        gateway.GetEmailsAsync(_club.Id, 0, Arg.Any<CancellationToken>()).Returns(new StaffEmailsPage(_club, [failed, failed with { Id = Guid.NewGuid(), Status = StaffEmailStatus.Sent }], 0, false));
        gateway.RetryEmailAsync(_club.Id, new(failed.Id, 7), Arg.Any<CancellationToken>()).Returns(new ClubReply(ClubReplyKind.Saved, "Queued for retry."));
        var page = context.Render<ClubEmails>(parameters => parameters.Add(value => value.ClubId, _club.Id));
        var button = page.FindAll(".record-list button").ShouldHaveSingleItem();
        await button.ClickAsync();
        await gateway.Received(1).RetryEmailAsync(_club.Id, new(failed.Id, 7), Arg.Any<CancellationToken>());
        page.Find(".notice[data-kind='success']").TextContent.ShouldBe("Queued for retry.");
    }
}
