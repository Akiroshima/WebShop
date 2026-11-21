using System.Collections.Concurrent;

using MurketApi.Application.Abstractions;

namespace MurketApi.Infrastructure.RateLimiting;

/// <summary>Скользящее окно событий в памяти процесса (ключ → моменты событий).</summary>
public sealed class SlidingWindowRateLimiter : IRateLimiter
{
    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> events = [];

    public int CountRecent(string key, TimeSpan window)
    {
        var now = DateTimeOffset.UtcNow;
        if (!events.TryGetValue(key, out var stamps))
        {
            return 0;
        }

        lock (stamps)
        {
            stamps.RemoveAll(stamp => now - stamp >= window);
            return stamps.Count;
        }
    }

    public void Register(string key)
    {
        var stamps = events.GetOrAdd(key, _ => []);
        lock (stamps)
        {
            stamps.Add(DateTimeOffset.UtcNow);
        }
    }
}
