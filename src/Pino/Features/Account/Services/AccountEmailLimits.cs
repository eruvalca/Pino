using System.Security.Cryptography;
using System.Text;

namespace Pino.Features.Account.Services;

internal sealed class AccountEmailLimits(TimeProvider time)
{
    private readonly Lock _gate = new();
    private readonly Dictionary<string, Queue<DateTimeOffset>> _attempts = new(StringComparer.Ordinal);

    internal bool TryReserve(string email, string purpose)
    {
        // Keep neither recipient addresses nor email contents in the bounded throttle cache.
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(purpose + "\n" + email.Trim().ToUpperInvariant())));
        var now = time.GetUtcNow();
        var cutoff = now.AddHours(-1);
        lock (_gate)
        {
            if (!_attempts.TryGetValue(key, out var attempts))
            {
                if (_attempts.Count >= 10_000)
                {
                    foreach (var expired in _attempts.Where(value => value.Value.Last() <= cutoff).Select(value => value.Key).ToArray()) { _attempts.Remove(expired); }
                    if (_attempts.Count >= 10_000) { return false; }
                }
                attempts = new();
                _attempts.Add(key, attempts);
            }
            while (attempts.TryPeek(out var first) && first <= cutoff) { attempts.Dequeue(); }
            if (attempts.Count >= 6 || (attempts.Count > 0 && now - attempts.Last() < TimeSpan.FromMinutes(1))) { return false; }
            // Reserve before SMTP, including failures, so retry storms cannot flood delivery.
            attempts.Enqueue(now);
            return true;
        }
    }
}
