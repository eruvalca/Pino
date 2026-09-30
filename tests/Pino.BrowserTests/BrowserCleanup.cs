using System.Diagnostics.CodeAnalysis;
using Xunit;

namespace Pino.BrowserTests;

internal static class BrowserCleanup
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Diagnostics are best-effort; all cleanup stages must run and their failures are rethrown together.")]
    internal static async Task RunAsync(Func<Task> capture, params Func<Task>[] stages)
    {
        try { await capture(); }
        catch (Exception exception) { TestContext.Current.TestOutputHelper?.WriteLine($"Final browser capture failed: {exception}"); }
        var failures = new List<Exception>();
        foreach (var stage in stages)
        {
            try { await stage(); }
            catch (Exception exception) { failures.Add(exception); }
        }
        if (failures.Count > 0) { throw new AggregateException("Browser fixture cleanup failed.", failures); }
    }
}
