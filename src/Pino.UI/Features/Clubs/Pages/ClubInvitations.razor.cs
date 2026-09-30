using Microsoft.AspNetCore.Components;
using Pino.SharedKernel.Clubs;
using Pino.UI.Features.Sporting.Services;

namespace Pino.UI.Features.Clubs.Pages;

public sealed partial class ClubInvitations : SportPageBase
{
    private InvitationsPage? _data;
    private InvitationInput _input = new();
    private bool _confirmAdministrator;
    private InvitationSummary? _pendingRevoke;
    private int _page;
    private ElementReference _confirmHeading;
    private bool _focusConfirmation;

    protected override async Task LoadAsync()
    {
        if (Membership?.Role != ClubRole.Administrator) { throw new UnauthorizedAccessException(); }
        _data = await ReadInitialAsync("GetInvitationsAsync", () => Clubs.GetInvitationsAsync(ClubId, _page, Token));
    }
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusConfirmation) { _focusConfirmation = false; await _confirmHeading.FocusAsync(); }
    }
    private static string State(InvitationSummary value)
    {
        if (value.UsedAt is not null) { return "Accepted"; }
        if (value.RevokedAt is not null) { return "Revoked"; }
        return value.ExpiresAt <= DateTimeOffset.UtcNow ? "Expired" : "Awaiting acceptance";
    }
    private async Task InviteAsync()
    {
        if (_input.Role == ClubRole.Administrator && !_confirmAdministrator) { return; }
        if (await RunAsync(() => Clubs.InviteAsync(ClubId, _input, Token)))
        {
            _input = new();
            _confirmAdministrator = false;
        }
    }
    private async Task ResendAsync(InvitationSummary value) => await RunAsync(() => Clubs.ResendInvitationAsync(ClubId, new(value.Id, value.Revision), Token));
    private void ConfirmRevoke(InvitationSummary value) { _pendingRevoke = value; _focusConfirmation = true; }
    private void CancelRevoke() => _pendingRevoke = null;
    private async Task RevokeAsync()
    {
        if (_pendingRevoke is not { } value) { return; }
        await RunAsync(() => Clubs.RevokeInvitationAsync(ClubId, new(value.Id, value.Revision), Token));
        _pendingRevoke = null;
    }
    private async Task<bool> RunAsync(Func<Task<ClubReply>> operation)
    {
        var saved = false;
        await ExecuteAsync(async () =>
        {
            var reply = await operation();
            saved = reply.Succeeded;
            Message = reply.Message;
            MessageKind = reply.Succeeded ? "success" : "error";
            await LoadAsync();
        });
        return saved;
    }
    private async Task PreviousAsync() { _page--; await ReloadAsync(); }
    private async Task NextAsync() { _page++; await ReloadAsync(); }
}
