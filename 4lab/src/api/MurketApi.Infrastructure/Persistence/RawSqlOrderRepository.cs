using Npgsql;

using MurketApi.Domain.Common;
using MurketApi.Domain.Orders;

namespace MurketApi.Infrastructure.Persistence;

/// <summary>
/// Репозиторий заказов на «сыром» SQL (ADO.NET: NpgsqlCommand/NpgsqlDataReader) — без EF Core.
/// Агрегат «заказ + позиции» читается двумя запросами (страница заказов + позиции по их id),
/// запись оформления — INSERT заказов и позиций в транзакции единицы работы.
/// </summary>
public sealed class RawSqlOrderRepository(PostgresStore store) : IOrderRepository
{
    public async Task<int> NextNumberAsync()
    {
        await store.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT nextval('order_number_seq')", store.Connection, store.Transaction);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task AddAsync(Order order)
    {
        // Вне единицы работы (например, из сида) — локальная транзакция на вставку заказа и позиций.
        NpgsqlTransaction? local = store.Transaction;
        if (local is null)
        {
            await store.OpenAsync();
            local = await store.Connection.BeginTransactionAsync();
        }

        try
        {
            await InsertOrderAsync(order, local);
            foreach (var item in order.Items)
            {
                await InsertItemAsync(order, item, local);
            }

            if (!ReferenceEquals(local, store.Transaction))
            {
                await local.CommitAsync();
                await local.DisposeAsync();
            }
        }
        catch
        {
            if (!ReferenceEquals(local, store.Transaction))
            {
                await local.RollbackAsync();
                await local.DisposeAsync();
            }

            throw;
        }
    }

    public async Task<Order?> FindByIdAsync(string id)
    {
        await store.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT o.id, o.number, o.user_id, o.status, o.created_at,
                   o.recipient_name, o.recipient_phone, o.recipient_email, o.comment,
                   p.code AS promo_code, o.subtotal, o.discount,
                   o.delivery_method, o.delivery, o.total,
                   o.addr_city, o.addr_street, o.addr_house, o.addr_apartment,
                   o.addr_entrance, o.addr_floor, o.addr_intercom, o.addr_postal_code
            FROM orders o
            LEFT JOIN promos p ON p.id = o.promo_id
            WHERE o.id = @id
            """, store.Connection, store.Transaction);
        command.Parameters.AddWithValue("id", Guid.Parse(id));

        await using var reader = await command.ExecuteReaderAsync();
        Order? order = null;
        if (await reader.ReadAsync())
        {
            order = ReadOrder(reader, reader.GetOrdinal("promo_code"));
        }

        // Ридер закрывается до второго запроса: на соединении не бывает двух активных курсоров.
        reader.Close();
        if (order is null)
        {
            return null;
        }

        await LoadItemsAsync([order]);
        return order;
    }

    public async Task<PagedResult<Order>> ForUserAsync(string userId, PageRequest page)
    {
        await store.OpenAsync();
        const string filter = "WHERE o.user_id = @user";
        await using var count = new NpgsqlCommand(
            $"SELECT count(*) FROM orders o {filter}", store.Connection, store.Transaction);
        count.Parameters.AddWithValue("user", Guid.Parse(userId));
        var total = Convert.ToInt32(await count.ExecuteScalarAsync());

        await using var select = new NpgsqlCommand(
            $"""
             SELECT o.id, o.number, o.user_id, o.status, o.created_at,
                    o.recipient_name, o.recipient_phone, o.recipient_email, o.comment,
                    p.code AS promo_code, o.subtotal, o.discount,
                    o.delivery_method, o.delivery, o.total,
                    o.addr_city, o.addr_street, o.addr_house, o.addr_apartment,
                    o.addr_entrance, o.addr_floor, o.addr_intercom, o.addr_postal_code
             FROM orders o
             LEFT JOIN promos p ON p.id = o.promo_id
             {filter}
             ORDER BY o.created_at DESC, o.number DESC
             OFFSET @skip LIMIT @take
             """, store.Connection, store.Transaction);
        select.Parameters.AddWithValue("user", Guid.Parse(userId));
        select.Parameters.AddWithValue("skip", page.Skip);
        select.Parameters.AddWithValue("take", page.PageSize);

        var orders = await ReadOrdersAsync(select);
        return new PagedResult<Order>(orders, total, page.Page, page.PageSize);
    }

    public async Task<PagedResult<Order>> AllAsync(OrderStatus? status, PageRequest page)
    {
        await store.OpenAsync();
        var filter = status is null ? string.Empty : "WHERE o.status = @status";
        await using var count = new NpgsqlCommand(
            $"SELECT count(*) FROM orders o {filter}", store.Connection, store.Transaction);
        if (status is not null)
        {
            count.Parameters.AddWithValue("status", StatusText(status.Value));
        }

        var total = Convert.ToInt32(await count.ExecuteScalarAsync());

        await using var select = new NpgsqlCommand(
            $"""
             SELECT o.id, o.number, o.user_id, o.status, o.created_at,
                    o.recipient_name, o.recipient_phone, o.recipient_email, o.comment,
                    p.code AS promo_code, o.subtotal, o.discount,
                    o.delivery_method, o.delivery, o.total,
                    o.addr_city, o.addr_street, o.addr_house, o.addr_apartment,
                    o.addr_entrance, o.addr_floor, o.addr_intercom, o.addr_postal_code
             FROM orders o
             LEFT JOIN promos p ON p.id = o.promo_id
             {filter}
             ORDER BY o.created_at DESC, o.number DESC
             OFFSET @skip LIMIT @take
             """, store.Connection, store.Transaction);
        if (status is not null)
        {
            select.Parameters.AddWithValue("status", StatusText(status.Value));
        }

        select.Parameters.AddWithValue("skip", page.Skip);
        select.Parameters.AddWithValue("take", page.PageSize);

        var orders = await ReadOrdersAsync(select);
        return new PagedResult<Order>(orders, total, page.Page, page.PageSize);
    }

    public async Task SaveStatusAsync(Order order)
    {
        await store.OpenAsync();
        await using var command = new NpgsqlCommand(
            "UPDATE orders SET status = @status WHERE id = @id", store.Connection, store.Transaction);
        command.Parameters.AddWithValue("status", StatusText(order.Status));
        command.Parameters.AddWithValue("id", Guid.Parse(order.Id));
        await command.ExecuteNonQueryAsync();
    }

    public async Task SetNextNumberAsync(int nextNumber)
    {
        await store.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT setval('order_number_seq', @value)", store.Connection, store.Transaction);
        command.Parameters.AddWithValue("value", nextNumber - 1);
        await command.ExecuteScalarAsync();
    }

    // ----- Вспомогательные операции -----

    private async Task InsertOrderAsync(Order order, NpgsqlTransaction transaction)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO orders (id, number, user_id, status, created_at,
                                recipient_name, recipient_phone, recipient_email, comment, promo_id,
                                subtotal, discount, delivery_method, delivery, total,
                                addr_city, addr_street, addr_house, addr_apartment,
                                addr_entrance, addr_floor, addr_intercom, addr_postal_code)
            VALUES (@id, @number, @user, @status, @createdAt,
                    @recipientName, @recipientPhone, @recipientEmail, @comment,
                    (SELECT id FROM promos WHERE code = @promoCode),
                    @subtotal, @discount, @deliveryMethod, @delivery, @total,
                    @city, @street, @house, @apartment, @entrance, @floor, @intercom, @postalCode)
            """, store.Connection, transaction);
        AddOrderParameters(command, order);
        await command.ExecuteNonQueryAsync();
    }

    private async Task InsertItemAsync(Order order, OrderItem item, NpgsqlTransaction transaction)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO order_items (id, order_id, product_id, name, sku, price, quantity)
            VALUES (@id, @orderId, @productId, @name, @sku, @price, @quantity)
            """, store.Connection, transaction);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("orderId", Guid.Parse(order.Id));
        command.Parameters.AddWithValue("productId",
            item.ProductId is null ? DBNull.Value : Guid.Parse(item.ProductId));
        command.Parameters.AddWithValue("name", item.Name);
        command.Parameters.AddWithValue("sku", item.Sku);
        command.Parameters.AddWithValue("price", item.UnitPrice);
        command.Parameters.AddWithValue("quantity", item.Quantity);
        await command.ExecuteNonQueryAsync();
    }

    private static void AddOrderParameters(NpgsqlCommand command, Order order)
    {
        command.Parameters.AddWithValue("id", Guid.Parse(order.Id));
        command.Parameters.AddWithValue("number", order.Number);
        command.Parameters.AddWithValue("user", Guid.Parse(order.UserId));
        command.Parameters.AddWithValue("status", StatusText(order.Status));
        command.Parameters.AddWithValue("createdAt", order.CreatedAt);
        command.Parameters.AddWithValue("recipientName", order.RecipientName);
        command.Parameters.AddWithValue("recipientPhone", order.RecipientPhone);
        command.Parameters.AddWithValue("recipientEmail", (object?)order.RecipientEmail ?? DBNull.Value);
        command.Parameters.AddWithValue("comment", (object?)order.Comment ?? DBNull.Value);
        command.Parameters.AddWithValue("promoCode", (object?)order.PromoCode ?? DBNull.Value);
        command.Parameters.AddWithValue("subtotal", order.Subtotal);
        command.Parameters.AddWithValue("discount", order.Discount);
        command.Parameters.AddWithValue("deliveryMethod", MethodText(order.DeliveryMethod));
        command.Parameters.AddWithValue("delivery", order.DeliveryPrice);
        command.Parameters.AddWithValue("total", order.Total);
        command.Parameters.AddWithValue("city", order.Address.City);
        command.Parameters.AddWithValue("street", order.Address.Street);
        command.Parameters.AddWithValue("house", order.Address.House);
        command.Parameters.AddWithValue("apartment", (object?)order.Address.Apartment ?? DBNull.Value);
        command.Parameters.AddWithValue("entrance", (object?)order.Address.Entrance ?? DBNull.Value);
        command.Parameters.AddWithValue("floor", (object?)order.Address.Floor ?? DBNull.Value);
        command.Parameters.AddWithValue("intercom", (object?)order.Address.Intercom ?? DBNull.Value);
        command.Parameters.AddWithValue("postalCode", (object?)order.Address.PostalCode ?? DBNull.Value);
    }

    private async Task<List<Order>> ReadOrdersAsync(NpgsqlCommand select)
    {
        var orders = new List<Order>();
        await using (var reader = await select.ExecuteReaderAsync())
        {
            var promoOrdinal = reader.GetOrdinal("promo_code");
            while (await reader.ReadAsync())
            {
                orders.Add(ReadOrder(reader, promoOrdinal));
            }
        }

        await LoadItemsAsync(orders);
        return orders;
    }

    /// <summary>Догружает позиции заказов одним запросом WHERE order_id = ANY(@ids).</summary>
    private async Task LoadItemsAsync(List<Order> orders)
    {
        if (orders.Count == 0)
        {
            return;
        }

        var byId = orders.ToDictionary(order => order.Id);
        await using var command = new NpgsqlCommand(
            """
            SELECT order_id, product_id, name, sku, price, quantity
            FROM order_items
            WHERE order_id = ANY(@ids)
            ORDER BY sku
            """, store.Connection, store.Transaction);
        command.Parameters.AddWithValue("ids", orders.Select(order => Guid.Parse(order.Id)).ToArray());

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var orderId = reader.GetGuid(0).ToString("D");
            if (!byId.TryGetValue(orderId, out var order))
            {
                continue;
            }

            var productId = reader.IsDBNull(1) ? null : reader.GetGuid(1).ToString("D");
            order.Items.Add(OrderItem.Create(
                productId,
                reader.GetString(2),
                reader.GetString(3),
                reader.GetDecimal(4),
                reader.GetInt32(5)));
        }
    }

    private static Order ReadOrder(NpgsqlDataReader reader, int promoOrdinal) => Order.Restore(
        id: reader.GetGuid(reader.GetOrdinal("id")).ToString("D"),
        number: reader.GetInt32(reader.GetOrdinal("number")),
        userId: reader.GetGuid(reader.GetOrdinal("user_id")).ToString("D"),
        status: ParseStatus(reader.GetString(reader.GetOrdinal("status"))),
        createdAt: reader.GetFieldValue<DateTimeOffset>(reader.GetOrdinal("created_at")),
        recipientName: reader.GetString(reader.GetOrdinal("recipient_name")),
        recipientPhone: reader.GetString(reader.GetOrdinal("recipient_phone")),
        recipientEmail: reader.IsDBNull(reader.GetOrdinal("recipient_email"))
            ? null
            : reader.GetString(reader.GetOrdinal("recipient_email")),
        address: AddressSnapshot.Create(
            reader.GetString(reader.GetOrdinal("addr_city")),
            reader.IsDBNull(reader.GetOrdinal("addr_postal_code"))
                ? null
                : reader.GetString(reader.GetOrdinal("addr_postal_code")),
            reader.GetString(reader.GetOrdinal("addr_street")),
            reader.GetString(reader.GetOrdinal("addr_house")),
            reader.IsDBNull(reader.GetOrdinal("addr_apartment"))
                ? null
                : reader.GetString(reader.GetOrdinal("addr_apartment")),
            reader.IsDBNull(reader.GetOrdinal("addr_entrance"))
                ? null
                : reader.GetString(reader.GetOrdinal("addr_entrance")),
            reader.IsDBNull(reader.GetOrdinal("addr_floor"))
                ? null
                : reader.GetString(reader.GetOrdinal("addr_floor")),
            reader.IsDBNull(reader.GetOrdinal("addr_intercom"))
                ? null
                : reader.GetString(reader.GetOrdinal("addr_intercom"))),
        comment: reader.IsDBNull(reader.GetOrdinal("comment"))
            ? null
            : reader.GetString(reader.GetOrdinal("comment")),
        promoCode: reader.IsDBNull(promoOrdinal) ? null : reader.GetString(promoOrdinal),
        subtotal: reader.GetDecimal(reader.GetOrdinal("subtotal")),
        discount: reader.GetDecimal(reader.GetOrdinal("discount")),
        deliveryMethod: ParseMethod(reader.GetString(reader.GetOrdinal("delivery_method"))),
        deliveryPrice: reader.GetDecimal(reader.GetOrdinal("delivery")),
        total: reader.GetDecimal(reader.GetOrdinal("total")),
        items: []);

    private static string StatusText(OrderStatus status) => status switch
    {
        OrderStatus.Confirmed => "confirmed",
        OrderStatus.Delivered => "delivered",
        OrderStatus.Cancelled => "cancelled",
        _ => "new",
    };

    private static OrderStatus ParseStatus(string value) => value switch
    {
        "confirmed" => OrderStatus.Confirmed,
        "delivered" => OrderStatus.Delivered,
        "cancelled" => OrderStatus.Cancelled,
        _ => OrderStatus.New,
    };

    private static string MethodText(DeliveryMethod method) =>
        method == DeliveryMethod.Courier ? "courier" : "pickup";

    private static DeliveryMethod ParseMethod(string value) =>
        value == "courier" ? DeliveryMethod.Courier : DeliveryMethod.Pickup;
}
