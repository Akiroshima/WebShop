using MurketApi.Domain.Common;

namespace MurketApi.Domain.Orders;

/// <summary>Позиция заказа — снимок товара на момент оформления.</summary>
public sealed class OrderItem
{
    /// <summary>null — товар удалён из каталога после оформления.</summary>
    public string? ProductId { get; internal set; }

    public string Name { get; internal set; } = string.Empty;

    public string Sku { get; internal set; } = string.Empty;

    public decimal UnitPrice { get; internal set; }

    public int Quantity { get; internal set; }

    public static OrderItem Create(string? productId, string name, string sku, decimal unitPrice, int quantity) =>
        new() { ProductId = productId, Name = name, Sku = sku, UnitPrice = unitPrice, Quantity = quantity };
}

/// <summary>Заказ: зафиксированный снимок состава, адреса и скидки на момент оформления.</summary>
public sealed class Order
{
    public string Id { get; private set; } = string.Empty;

    public int Number { get; private set; }

    public string UserId { get; private set; } = string.Empty;

    public OrderStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string RecipientName { get; private set; } = string.Empty;

    public string RecipientPhone { get; private set; } = string.Empty;

    public string? RecipientEmail { get; private set; }

    /// <summary>Снимок адреса доставки.</summary>
    public AddressSnapshot Address { get; private set; } = null!;

    public string? Comment { get; private set; }

    /// <summary>Снимок кода промокода или null.</summary>
    public string? PromoCode { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal Discount { get; private set; }

    public DeliveryMethod DeliveryMethod { get; private set; }

    public decimal DeliveryPrice { get; private set; }

    public decimal Total { get; private set; }

    public List<OrderItem> Items { get; private set; } = [];

    private Order()
    {
    }

    /// <summary>Создаёт заказ (статус New). Списание остатков и очистку корзины выполняет прикладной слой.</summary>
    public static Order Place(string id, int number, string userId, string recipientName, string recipientPhone,
        string? recipientEmail, AddressSnapshot address, string? comment, DeliveryMethod deliveryMethod,
        string? promoCode, decimal subtotal, decimal discount, IReadOnlyList<OrderItem> items,
        DateTimeOffset createdAt)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(recipientName))
        {
            errors["recipientName"] = ["Укажите имя получателя"];
        }

        if (string.IsNullOrWhiteSpace(recipientPhone))
        {
            errors["recipientPhone"] = ["Укажите телефон"];
        }

        if (address is null || !address.IsFilled)
        {
            errors["address"] = ["Укажите город, улицу и дом"];
            throw new ValidationException("Данные заполнены неверно", errors);
        }

        if (items.Count == 0)
        {
            throw new ConflictException("Корзина пуста");
        }

        if (discount < 0 || discount > subtotal)
        {
            throw new ConflictException("Скидка не может превышать сумму товаров");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Данные заполнены неверно", errors);
        }

        var delivery = DeliveryCatalog.PriceOf(deliveryMethod);
        return new Order
        {
            Id = id,
            Number = number,
            UserId = userId,
            Status = OrderStatus.New,
            CreatedAt = createdAt,
            RecipientName = recipientName.Trim(),
            RecipientPhone = Phone.Normalize(recipientPhone),
            RecipientEmail = string.IsNullOrWhiteSpace(recipientEmail) ? null : recipientEmail.Trim(),
            Address = address,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            PromoCode = string.IsNullOrWhiteSpace(promoCode) ? null : promoCode.Trim().ToUpperInvariant(),
            Subtotal = subtotal,
            Discount = discount,
            DeliveryMethod = deliveryMethod,
            DeliveryPrice = delivery,
            Total = Money.Round(subtotal - discount + delivery),
            Items = [.. items],
        };
    }

    /// <summary>
    /// Восстановление из хранилища (репозиторий заказов на «сыром» SQL): снимки уже зафиксированы,
    /// повторная валидация не нужна — состав полей обязан совпадать со строкой таблицы orders.
    /// </summary>
    internal static Order Restore(string id, int number, string userId, OrderStatus status,
        DateTimeOffset createdAt, string recipientName, string recipientPhone, string? recipientEmail,
        AddressSnapshot address, string? comment, string? promoCode, decimal subtotal, decimal discount,
        DeliveryMethod deliveryMethod, decimal deliveryPrice, decimal total, List<OrderItem> items) =>
        new()
        {
            Id = id,
            Number = number,
            UserId = userId,
            Status = status,
            CreatedAt = createdAt,
            RecipientName = recipientName,
            RecipientPhone = recipientPhone,
            RecipientEmail = recipientEmail,
            Address = address,
            Comment = comment,
            PromoCode = promoCode,
            Subtotal = subtotal,
            Discount = discount,
            DeliveryMethod = deliveryMethod,
            DeliveryPrice = deliveryPrice,
            Total = total,
            Items = items,
        };

    /// <summary>Переход статуса по политике; недопустимый переход — конфликт.</summary>
    public void TransitionTo(OrderStatus next)
    {
        if (!OrderStatusPolicy.CanTransition(Status, next))
        {
            throw new ConflictException("Недопустимый переход статуса заказа");
        }

        Status = next;
    }

    /// <summary>Снимки позиций для возврата остатков при отмене.</summary>
    public IEnumerable<(string ProductId, int Quantity)> ReturnableItems() =>
        Items.Where(item => item.ProductId is not null)
            .Select(item => (item.ProductId!, item.Quantity));
}

/// <summary>Снимок адреса доставки в заказе.</summary>
public sealed class AddressSnapshot
{
    public string City { get; internal set; } = string.Empty;

    public string? PostalCode { get; internal set; }

    public string Street { get; internal set; } = string.Empty;

    public string House { get; internal set; } = string.Empty;

    public string? Apartment { get; internal set; }

    public string? Entrance { get; internal set; }

    public string? Floor { get; internal set; }

    public string? Intercom { get; internal set; }

    public static AddressSnapshot Create(string city, string? postalCode, string street, string house,
        string? apartment, string? entrance, string? floor, string? intercom) => new()
    {
        City = city,
        PostalCode = postalCode,
        Street = street,
        House = house,
        Apartment = apartment,
        Entrance = entrance,
        Floor = floor,
        Intercom = intercom,
    };

    /// <summary>Обязательная часть адреса заполнена.</summary>
    public bool IsFilled =>
        !string.IsNullOrWhiteSpace(City) && !string.IsNullOrWhiteSpace(Street) && !string.IsNullOrWhiteSpace(House);
}
