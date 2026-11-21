namespace MurketApi.Domain.Orders;

public enum DeliveryMethod
{
    Courier = 0,
    Pickup,
}

/// <summary>Способы доставки и их стоимость (фиксирована бизнесом на v1).</summary>
public static class DeliveryCatalog
{
    public static readonly IReadOnlyDictionary<DeliveryMethod, decimal> Prices =
        new Dictionary<DeliveryMethod, decimal>
        {
            [DeliveryMethod.Courier] = 290m,
            [DeliveryMethod.Pickup] = 190m,
        };

    public static decimal PriceOf(DeliveryMethod method) =>
        Prices.TryGetValue(method, out var price) ? price : Prices[DeliveryMethod.Courier];
}
