using MurketApi.Domain.Common;

namespace MurketApi.Domain.Orders;

/// <summary>
/// Заказы. Реализация — «сырой» SQL через ADO.NET (NpgsqlCommand/NpgsqlDataReader):
/// агрегат «заказ + позиции» читается и пишется явными запросами к PostgreSQL.
/// </summary>
public interface IOrderRepository
{
    /// <summary>Выдаёт и фиксирует следующий порядковый номер заказа (sequence order_number_seq).</summary>
    Task<int> NextNumberAsync();

    Task AddAsync(Order order);

    Task<Order?> FindByIdAsync(string id);

    /// <summary>Заказы пользователя (новые сверху) — постранично.</summary>
    Task<PagedResult<Order>> ForUserAsync(string userId, PageRequest page);

    /// <summary>Все заказы магазина с фильтром по статусу — постранично (модуль менеджера).</summary>
    Task<PagedResult<Order>> AllAsync(OrderStatus? status, PageRequest page);

    /// <summary>Сохраняет статус заказа после перехода (снимки позиций не меняются).</summary>
    Task SaveStatusAsync(Order order);

    /// <summary>Сдвигает счётчик номеров заказов за уже существующие (используется сидом).</summary>
    Task SetNextNumberAsync(int nextNumber);
}
