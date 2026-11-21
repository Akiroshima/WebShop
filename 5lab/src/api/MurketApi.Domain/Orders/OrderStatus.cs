namespace MurketApi.Domain.Orders;

/// <summary>Статус заказа; после создания у заказа меняется только статус.</summary>
public enum OrderStatus
{
    New = 0,
    Confirmed,
    Delivered,
    Cancelled,
}

/// <summary>Доменная политика переходов статусов (единая для покупателя и менеджера).</summary>
public static class OrderStatusPolicy
{
    private static readonly IReadOnlyDictionary<OrderStatus, OrderStatus[]> Transitions =
        new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.New] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
            [OrderStatus.Confirmed] = [OrderStatus.Delivered, OrderStatus.Cancelled],
            [OrderStatus.Delivered] = [],
            [OrderStatus.Cancelled] = [],
        };

    public static bool CanTransition(OrderStatus from, OrderStatus to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
