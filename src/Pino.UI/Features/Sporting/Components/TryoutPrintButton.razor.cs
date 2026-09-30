using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Pino.UI.Features.Sporting.Components;

public sealed partial class TryoutPrintButton(IJSRuntime js) : IAsyncDisposable
{
    [Parameter] public bool Disabled { get; set; }
    private readonly PrintInterop _print = new(js);
    private bool _printing;
    private bool _failed;

    private async Task PrintAsync()
    {
        if (Disabled || _printing) { return; }
        _printing = true;
        _failed = false;
        try { await _print.PrintAsync(); }
        catch (JSException) { _failed = true; }
        catch (TaskCanceledException) { _failed = true; }
        finally { _printing = false; }
    }

    public ValueTask DisposeAsync() => _print.DisposeAsync();

    private sealed class PrintInterop(IJSRuntime runtime) : IAsyncDisposable
    {
        private const string ModulePath = "./_content/Pino.UI/Features/Sporting/Components/TryoutPrintButton.razor.js";
        private const string PrintMethod = "printDocument";
        private IJSObjectReference? _module;

        internal async Task PrintAsync()
        {
            _module ??= await runtime.InvokeAsync<IJSObjectReference>("import", ModulePath);
            await _module.InvokeVoidAsync(PrintMethod);
        }

        public async ValueTask DisposeAsync()
        {
            try { if (_module is not null) { await _module.DisposeAsync(); } }
            catch (JSDisconnectedException) { /* The browser circuit ended before disposal. */ }
        }
    }
}
