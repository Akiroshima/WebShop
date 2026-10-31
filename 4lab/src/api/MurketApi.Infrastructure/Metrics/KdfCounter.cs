using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using MurketApi.Application.Abstractions;

namespace MurketApi.Infrastructure.Metrics;

/// <summary>
/// Метрика auth_kdf_operations_total{caller}: инкрементируется каждым PBKDF2-вызовом;
/// суммарное значение по всем caller логируется раз в 60 секунд.
/// </summary>
public sealed class KdfCounter : Application.Abstractions.IKdfCounter, IDisposable
{
    private readonly ConcurrentDictionary<string, int> counters = [];
    private readonly ILogger<KdfCounter> logger;
    private readonly Timer timer;

    public KdfCounter(ILogger<KdfCounter> logger)
    {
        this.logger = logger;
        timer = new Timer(_ => LogTotals(), null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));
    }

    public void Increment(string caller) => counters.AddOrUpdate(caller, 1, (_, value) => value + 1);

    private void LogTotals()
    {
        var total = counters.Values.Sum();
        if (total == 0)
        {
            return;
        }

        var perCaller = string.Join(", ", counters.OrderBy(pair => pair.Key)
            .Select(pair => $"{pair.Key}={pair.Value}"));
        logger.LogInformation("auth_kdf_operations_total{{caller}}: total={Total} ({PerCaller})", total, perCaller);
    }

    public void Dispose() => timer.Dispose();
}
