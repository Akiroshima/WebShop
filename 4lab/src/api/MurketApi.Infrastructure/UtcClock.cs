using MurketApi.Domain.Common;

namespace MurketApi.Infrastructure;

/// <summary>Системные часы (UTC).</summary>
public sealed class UtcClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
