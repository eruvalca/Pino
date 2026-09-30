using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Pino.SharedKernel.Clubs;
using Pino.UI.Services;

namespace Pino.UI.Features.Clubs.Pages;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The UI boundary logs server or transport failures and retains editable input for retry.")]
public sealed partial class ClubAccess(IClubGateway gateway, NavigationManager navigation, ILogger<ClubAccess> logger) : IDisposable
{
    [Inject] private PersistentComponentState PageState { get; set; } = default!;
    [Inject] private NavigationManager PageNavigation { get; set; } = default!;
    private InitialPageState? _initialState;
    private AccessSnapshot? _access;
    private ClubSearchPage? _results;
    private CreateClubInput _club = new();
    private ElementReference _heading;
    private ElementReference _taskHeading;
    private string _query = "";
    private string _searched = "";
    private string? _message;
    private string _kind = "information";
    private bool _busy;
    private bool IsBusy => _busy || !RendererInfo.IsInteractive;
    private bool _failed;
    private bool _editing;
    private bool _creating;
    private bool _reviewClub;
    private ClubSummary? _joinClub;
    private static readonly string[] _setupSteps = ["Your profile", "Choose a club", "Review", "Club access"];
    private int SetupStep
    {
        get
        {
            if (_access?.Profile.IsComplete != true || _editing) { return 0; }
            if (_access.Request is { Status: JoinRequestStatus.Pending }) { return 3; }
            return _reviewClub || _joinClub is not null ? 2 : 1;
        }
    }
    private void ReviewCreate() { _reviewClub = true; _focusHeading = true; }
    private void BackToClubDetails() { _reviewClub = false; _focusHeading = true; }
    private void ReviewJoin(ClubSummary club) { _joinClub = club; _focusHeading = true; }

    private bool _confirmLeave;
    private bool _focusHeading;
    private bool _editProfileRequestHandled;
    [SupplyParameterFromQuery(Name = "editProfile")] private bool EditProfileRequested { get; set; }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);
        if (_initialState is null)
        {
            // Keep prerendered content in place until the first interactive read finishes.
            _initialState = new(PageState, $"ClubAccess:{PageNavigation.Uri.Split('#')[0]}", RendererInfo.IsInteractive);
            await RefreshAsync();
            _initialState.Complete(!_failed);
        }
        await base.SetParametersAsync(ParameterView.Empty);
        var path = new Uri(navigation.Uri).AbsolutePath.TrimEnd('/');
        if (string.Equals(path, "/club", StringComparison.OrdinalIgnoreCase) && _access is { Profile.IsComplete: true, Membership: { } member })
        {
            navigation.NavigateTo($"/clubs/{member.Club.Id}", replace: true);
        }
    }

    protected override void OnParametersSet()
    {
        if (EditProfileRequested && !_editProfileRequestHandled) { EditProfile(); }
        _editProfileRequestHandled = EditProfileRequested;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_focusHeading)
        {
            _focusHeading = false;
            var heading = _access is { Profile.IsComplete: true } && !_editing ? _taskHeading : _heading;
            await heading.FocusAsync();
        }
    }

    private static string RoleLabel(ClubRole role) => role == ClubRole.Administrator ? "an administrator" : "a coach";
    private void EditProfile() { _editing = true; _focusHeading = true; }
    private void StopEditing() { _editing = false; _focusHeading = true; }
    private void ShowCreate() { _creating = true; _reviewClub = false; _joinClub = null; _message = null; _focusHeading = true; }
    private void ShowSearch() { _creating = false; _reviewClub = false; _joinClub = null; _focusHeading = true; }
    private void ConfirmLeave() => _confirmLeave = true;
    private void CancelLeave() => _confirmLeave = false;
    private Task RefreshQuietlyAsync() => _busy ? Task.CompletedTask : RefreshAsync();

    private async Task RefreshAsync()
    {
        try
        {
            _access = await (_initialState is null ? gateway.GetAccessAsync() : _initialState.ReadAsync("access", () => gateway.GetAccessAsync()));
            _failed = false;
        }
        catch (Exception exception)
        {
            if (exception is UnauthorizedAccessException) { _access = null; }
            _failed = true;
            ReportFailure(exception);
        }
    }

    private Task SaveProfileAsync(ProfileInput input) => RunAsync(() => gateway.SaveProfileAsync(input), closeProfile: true);
    private Task CreateAsync() => RunAsync(() => gateway.CreateAsync(_club));
    private Task RequestAsync(Guid clubId) => RunAsync(() => gateway.RequestAsync(clubId));
    private Task CancelRequestAsync(Guid requestId) => RunAsync(() => gateway.CancelAsync(requestId));
    private Task LeaveAsync(Guid clubId) => RunAsync(() => gateway.LeaveAsync(clubId));

    private async Task RunAsync(Func<Task<ClubReply>> operation, bool closeProfile = false)
    {
        if (_busy) { return; }
        _busy = true;
        try
        {
            var reply = await operation();
            _message = reply.Message;
            _kind = reply.Succeeded ? "success" : "error";
            if (reply.Succeeded)
            {
                _editing = !closeProfile && _editing;
                _creating = false;
                _reviewClub = false;
                _joinClub = null;
                _confirmLeave = false;
                _club = new();
                _focusHeading = true;
            }
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            ReportFailure(exception);
            if (exception is UnauthorizedAccessException) { _access = null; _failed = true; }
        }
        finally { _busy = false; }
    }

    private Task SearchAsync() => SearchPageAsync(0);
    private Task PreviousAsync() => SearchPageAsync((_results?.Page ?? 1) - 1);
    private Task NextAsync() => SearchPageAsync((_results?.Page ?? 0) + 1);

    private async Task SearchPageAsync(int page)
    {
        _busy = true;
        _message = null;
        try
        {
            if (page == 0) { _searched = _query.Trim(); }
            _results = await gateway.SearchAsync(_searched, page);
        }
        catch (Exception exception) { _results = null; ReportFailure(exception); }
        finally { _busy = false; }
    }

    private void ReportFailure(Exception exception)
    {
        LogOperationFailed(logger, exception);
        _message = exception is UnauthorizedAccessException
            ? "Your club access or sign-in changed. Sign in again to continue."
            : "We couldn't confirm this operation. Your text is still here. Check your current status before trying again.";
        _kind = "error";
    }

    [LoggerMessage(EventId = 2010, Level = LogLevel.Warning, Message = "Club access operation failed.")]
    private static partial void LogOperationFailed(ILogger logger, Exception exception);
    public void Dispose() => _initialState?.Dispose();
}
