using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Pino.UI.Services;

// Transfers authorized read models from prerendering into this page's first
// interactive render. It never retains edits or answers a later refresh/write.
internal sealed class InitialPageState(PersistentComponentState state, string scope, bool interactive) : IDisposable
{
    private readonly string _scope = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(scope)));
    private readonly Dictionary<string, Action> _snapshots = new(StringComparer.Ordinal);
    private PersistingComponentStateSubscription _subscription;
    private bool _complete;

    internal async Task<T> ReadAsync<T>(string key, Func<Task<T>> read)
    {
        if (_complete) { return await read(); }
        var scopedKey = $"pino:{_scope}:{key}";
        if (interactive && state.TryTakeFromJson<T>(scopedKey, out var restored)) { return restored!; }
        var value = await read();
        if (!interactive) { _snapshots.Add(scopedKey, () => state.PersistAsJson(scopedKey, value)); }
        return value;
    }

    internal void Complete(bool succeeded)
    {
        if (_complete) { return; }
        _complete = true;
        if (interactive || !succeeded) { _snapshots.Clear(); return; }
        // Register only after the whole page has loaded, as required by Blazor.
        _subscription = state.RegisterOnPersisting(() =>
        {
            foreach (var persist in _snapshots.Values) { persist(); }
            return Task.CompletedTask;
        }, RenderMode.InteractiveAuto);
    }

    public void Dispose() { _subscription.Dispose(); _snapshots.Clear(); }
}
