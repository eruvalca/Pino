using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Pino.SharedKernel.Clubs;

namespace Pino.UI.Features.Clubs.Pages;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The page logs failures and clears protected member data when access cannot be verified.")]
public sealed partial class ClubPeople(IClubGateway gateway, ILogger<ClubPeople> logger)
{
    private PeoplePage? _data;
    private bool _requests = true;
    private bool _busy;
    private bool _loading;
    private long _loadVersion;
    private bool IsBusy => _busy || _loading || !RendererInfo.IsInteractive;
    private bool _failed;
    private int _page;
    private string? _message;
    private string _kind = "information";
    private Confirmation? _confirmation;
    private ElementReference _heading;
    private ElementReference _confirmHeading;
    private bool _focusConfirmation;

    [Parameter] public Guid ClubId { get; set; }

    protected override Task OnParametersSetAsync() => LoadAsync();
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusConfirmation)
        {
            _focusConfirmation = false;
            await _confirmHeading.FocusAsync();
        }
    }

    private Task RefreshQuietlyAsync() => IsBusy ? Task.CompletedTask : LoadAsync();

    private Task LoadAsync() => LoadPageAsync(_requests, _page);

    private async Task LoadPageAsync(bool requests, int page)
    {
        var version = ++_loadVersion;
        var clubId = ClubId;
        _loading = true;
        _failed = false;
        if (requests != _requests || page != _page || _data?.Club.Id != clubId)
        {
            _confirmation = null;
            _focusConfirmation = false;
        }
        if (_data?.Club.Id != clubId)
        {
            _data = null;
        }
        try
        {
            var data = await gateway.GetPeopleAsync(clubId, requests, page);
            if (version != _loadVersion) { return; }
            // Commit the view and its rows together; previous rows keep their own actions while loading.
            _data = data;
            _requests = requests;
            _page = page;
            _failed = false;
        }
        catch (Exception exception)
        {
            if (version != _loadVersion) { return; }
            _data = null;
            _confirmation = null;
            _failed = true;
            Failure(exception);
        }
        finally
        {
            if (version == _loadVersion) { _loading = false; }
        }
    }

    private Task ShowRequestsAsync() => IsBusy ? Task.CompletedTask : LoadPageAsync(true, 0);
    private Task ShowMembersAsync() => IsBusy ? Task.CompletedTask : LoadPageAsync(false, 0);
    private Task PreviousAsync() => IsBusy ? Task.CompletedTask : LoadPageAsync(_requests, _page - 1);
    private Task NextAsync() => IsBusy ? Task.CompletedTask : LoadPageAsync(_requests, _page + 1);
    private Task ApproveAsync(PersonSummary person) => RunAsync(() => gateway.DecideAsync(ClubId, new(person.RequestId!.Value, true)));

    private void Deny(PersonSummary person) => ShowConfirmation(new(
        $"Deny {person.FirstName} {person.LastName}'s request?", $"Access to {_data!.Club.Name} will not be granted. This person may reapply.",
        "Deny request", () => gateway.DecideAsync(ClubId, new(person.RequestId!.Value, false))));

    private void ChangeRole(PersonSummary person)
    {
        var role = person.Role == ClubRole.Coach ? ClubRole.Administrator : ClubRole.Coach;
        var label = role == ClubRole.Administrator ? "administrator" : "coach";
        ShowConfirmation(new($"Make {person.FirstName} {person.LastName} {label}?",
            role == ClubRole.Administrator ? $"They will be able to approve requests, change roles, and remove members of {_data!.Club.Name}." : $"They will keep coach access to {_data!.Club.Name} and will no longer manage members.",
            $"Make {label}", () => gateway.ChangeMemberAsync(ClubId, new(person.UserId, person.Role, role))));
    }

    private void Remove(PersonSummary person) => ShowConfirmation(new(
        $"Remove {person.FirstName} {person.LastName} from {_data!.Club.Name}?",
        "Their club access will end. Their Pino account and the club's records will remain. They may reapply.",
        "Remove from club", () => gateway.ChangeMemberAsync(ClubId, new(person.UserId, person.Role, null))));

    private void ShowConfirmation(Confirmation confirmation) { _confirmation = confirmation; _focusConfirmation = true; }
    private Task ConfirmAsync() => _confirmation is null ? Task.CompletedTask : RunAsync(_confirmation.Operation);
    private async Task CancelConfirmationAsync() { _confirmation = null; await _heading.FocusAsync(); }

    private async Task RunAsync(Func<Task<ClubReply>> operation)
    {
        if (IsBusy) { return; }
        _busy = true;
        try
        {
            var reply = await operation();
            _message = reply.Message;
            _kind = reply.Succeeded ? "success" : "error";
            _confirmation = null;
            await LoadAsync();
            await _heading.FocusAsync();
        }
        catch (Exception exception) { Failure(exception); await LoadAsync(); }
        finally { _busy = false; }
    }

    private void Failure(Exception exception)
    {
        LogPeopleFailed(logger, exception);
        _message = exception is UnauthorizedAccessException ? "You no longer have administrator access to this club." : "We couldn't confirm the change. Refresh to see the current state before trying again.";
        _kind = "error";
    }

    [LoggerMessage(EventId = 2011, Level = LogLevel.Warning, Message = "Club people operation failed.")]
    private static partial void LogPeopleFailed(ILogger logger, Exception exception);

    private sealed record Confirmation(string Title, string Description, string Action, Func<Task<ClubReply>> Operation);
}
