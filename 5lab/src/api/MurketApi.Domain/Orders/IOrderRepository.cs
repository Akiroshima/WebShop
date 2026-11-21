using MurketApi.Domain.Common;

namespace MurketApi.Domain.Orders;

/// <summary>Заказы. Заглушка БД: InMemoryOrderRepository.</summary>
public interface IOrderRepository
{
    void Add(Order order);

    Order? FindById(string id);

    /// <summary>Заказы пользователя (новые сверху) — постранично.</summary>
    PagedResult<Order> ForUser(string userId, PageRequest page);

    /// <summary>Все заказы магазина с фильтром по статусу — постранично (модуль менеджера).</summary>
    PagedResult<Order> All(OrderStatus? status, PageRequest page);

    /// <summary>Выдаёт и фиксирует следующий порядковый номер заказа.</summary>
    int NextNumber();
}
