using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Pino.SharedKernel.Clubs;
using Pino.UI.Services;

namespace Pino.UI.Features.Clubs.Pages;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The UI boundary clears protected invitation data on failure and offers an explicit retry. Logs exclude tokens.")]
public sealed partial class AcceptInvitation(IClubGateway gateway, ILogger<AcceptInvitation> logger) : IAsyncDisposable
{
    [Inject] private PersistentComponentState PageState { get; set; } = default!;
    [Inject] private NavigationManager PageNavigation { get; set; } = default!;
    private InitialPageState? _initialState;
    private readonly CancellationTokenSource _lifetime = new();
    private InvitationPreview? _preview;
    private AccessSnapshot? _access;
    private string? _message;
    private string _kind = "information";
    private bool Busy { get; set; }
    private bool _failed;
    private long _version;
    private bool IsBusy => Busy || !RendererInfo.IsInteractive;
    [Parameter] public Guid InvitationId { get; set; }
    [SupplyParameterFromQuery(Name = "token")] public string? InvitationToken { get; set; }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        // Keep prerendered content in place until the first interactive read finishes.
        parameters.SetParameterProperties(this);
        _initialState ??= new(PageState, $"Invitation:{PageNavigation.Uri.Split('#')[0]}", RendererInfo.IsInteractive);
        await LoadAsync();
        _initialState.Complete(!_failed);
        await base.SetParametersAsync(ParameterView.Empty);
    }
    private async Task LoadAsync()
    {
        var version = ++_version;
        var invitationId = InvitationId;
        var token = InvitationToken ?? "";
        Busy = true;
        _failed = false;
        _preview = null;
        _access = null;
        try
        {
            var preview = await (_initialState is null ? gateway.PreviewInvitationAsync(invitationId, token, _lifetime.Token) : _initialState.ReadAsync("preview", () => gateway.PreviewInvitationAsync(invitationId, token, _lifetime.Token)));
            var access = await (_initialState is null ? gateway.GetAccessAsync(_lifetime.Token) : _initialState.ReadAsync("access", () => gateway.GetAccessAsync(_lifetime.Token)));
            if (version != _version) { return; }
            _preview = preview;
            _access = access;
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { /* Navigation ended this request. */ }
        catch (Exception exception) { if (version == _version) { Failure(exception); } }
        finally { if (version == _version) { Busy = false; } }
    }

    private Task SaveProfileAsync(ProfileInput input) => RunAsync(() => gateway.SaveProfileAsync(input, _lifetime.Token));
    private Task AcceptAsync() => RunAsync(() => gateway.AcceptInvitationAsync(InvitationId, new(InvitationToken ?? ""), _lifetime.Token));
    private async Task RunAsync(Func<Task<ClubReply>> operation)
    {
        if (IsBusy) { return; }
        Busy = true;
        try
        {
            var reply = await operation();
            _message = reply.Message;
            _kind = reply.Succeeded ? "success" : "error";
            await LoadAsync();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { /* Navigation ended this request. */ }
        catch (Exception exception) { Failure(exception); }
        finally { Busy = false; }
    }
    private void Failure(Exception exception)
    {
        LogInvitationFailure(logger, exception.GetType().Name);
        _preview = null;
        _access = null;
        _failed = true;
        _message = "Your invitation could not be checked. Refresh or sign in with the invited, verified email address.";
        _kind = "error";
    }
    [LoggerMessage(EventId = 2042, Level = LogLevel.Warning, Message = "Invitation page operation failed ({FailureType}).")]
    private static partial void LogInvitationFailure(ILogger logger, string failureType);

    public async ValueTask DisposeAsync()
    {
        _initialState?.Dispose();
        await _lifetime.CancelAsync();
        _lifetime.Dispose();
    }
}
