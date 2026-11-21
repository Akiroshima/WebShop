namespace MurketApi.Domain.Common;

/// <summary>Единственный источник текущего времени (тестируется через фейк в юнит-тестах).</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
