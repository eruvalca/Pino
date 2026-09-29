using Microsoft.AspNetCore.Components;

namespace Pino.UI.Features.Clubs.Components;

public sealed partial class AccessWatch : IAsyncDisposable
{
    private readonly CancellationTokenSource _stopping = new();
    private Task? _watch;

    [Parameter, EditorRequired]
    public EventCallback Tick { get; set; }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _watch = WatchAsync();
        }
    }

    private async Task WatchAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            while (await timer.WaitForNextTickAsync(_stopping.Token))
            {
                await InvokeAsync(() => Tick.InvokeAsync());
            }
        }
        catch (OperationCanceledException) when (_stopping.IsCancellationRequested)
        {
            // Leaving the page ends access refreshes.
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stopping.CancelAsync();
        if (_watch is not null)
        {
            await _watch;
        }
        _stopping.Dispose();
    }
}
