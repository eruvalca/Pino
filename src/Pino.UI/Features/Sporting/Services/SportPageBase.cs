using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Pino.SharedKernel.Clubs;
using Pino.SharedKernel.Sporting;

namespace Pino.UI.Features.Sporting.Services;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Public Razor pages inherit this shared loading and mutation boundary.")]
[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The UI boundary logs failures and preserves editable input for retry; it clears protected content after denied access.")]
public abstract partial class SportPageBase : ComponentBase, IAsyncDisposable
{
    private readonly CancellationTokenSource _lifetime = new();
    [Inject] protected ISportGateway Gateway { get; set; } = default!;
    [Inject] protected IClubGateway Clubs { get; set; } = default!;
    [Inject] protected NavigationManager Navigation { get; set; } = default!;
    [Inject] private ILogger<SportPageBase> Logger { get; set; } = default!;
    [Parameter] public Guid ClubId { get; set; }
    protected MembershipSummary? Membership { get; private set; }
    protected bool Busy { get; private set; }
    protected bool Failed { get; private set; }
    protected bool Disabled => Busy || !RendererInfo.IsInteractive;
    protected string? Message { get; set; }
    protected string MessageKind { get; set; } = "information";
    protected CancellationToken Token => _lifetime.Token;

    protected override Task OnParametersSetAsync() => ReloadAsync();
    protected abstract Task LoadAsync();

    protected async Task ReloadAsync()
    {
        if (Busy) { return; }
        Busy = true;
        Failed = false;
        try
        {
            var access = await Clubs.GetAccessAsync(Token);
            Membership = access.Membership?.Club.Id == ClubId ? access.Membership : null;
            _ = Membership ?? throw new UnauthorizedAccessException();
            await LoadAsync();
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { /* Navigation ended this page's request. */ }
        catch (Exception exception) { Failed = true; ReportFailure(exception); }
        finally { Busy = false; }
    }

    protected async Task<bool> SaveAsync(Func<Task<SportReply>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        var succeeded = false;
        await ExecuteAsync(async () =>
        {
            var reply = await operation();
            Message = reply.Message;
            MessageKind = reply.Kind == SportReplyKind.Saved ? "success" : "error";
            succeeded = reply.Kind == SportReplyKind.Saved;
        });
        return succeeded;
    }

    protected async Task ExecuteAsync(Func<Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (Disabled) { return; }
        Busy = true;
        try { await operation(); }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested) { /* Navigation ended this page's request. */ }
        catch (Exception exception) { ReportFailure(exception); }
        finally { Busy = false; }
    }

    private void ReportFailure(Exception exception)
    {
        LogFailure(Logger, exception);
        MessageKind = "error";
        if (exception is UnauthorizedAccessException)
        {
            Membership = null;
            Failed = true;
            Message = "Your club access has changed. Open Your club to check your membership.";
        }
        else if (exception is KeyNotFoundException) { Failed = true; Message = "This record is unavailable. Return to the club workspace or reload."; }
        else { Message = "We couldn't complete this request. Your unsaved input is still here. Check your connection and try again."; }
    }

    [LoggerMessage(EventId = 3001, Level = LogLevel.Warning, Message = "Sporting workspace request failed.")]
    private static partial void LogFailure(ILogger logger, Exception exception);

    public async ValueTask DisposeAsync()
    {
        await _lifetime.CancelAsync();
        _lifetime.Dispose();
        GC.SuppressFinalize(this);
    }
}
