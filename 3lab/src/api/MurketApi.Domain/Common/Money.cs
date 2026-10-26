namespace MurketApi.Domain.Common;

/// <summary>Операции с деньгами: копейки — минимальная единица, округление «от нуля».</summary>
public static class Money
{
    /// <summary>Округляет сумму до копеек (mode AwayFromZero — как Math.round в клиенте).</summary>
    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
