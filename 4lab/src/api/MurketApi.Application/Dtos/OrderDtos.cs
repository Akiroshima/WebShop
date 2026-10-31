namespace MurketApi.Application.Dtos;

/// <summary>Адрес в заказе (снимок на момент оформления).</summary>
public sealed record OrderAddressDto(
    string City,
    string? PostalCode,
    string Street,
    string House,
    string? Apartment,
    string? Entrance,
    string? Floor,
    string? Intercom);

/// <summary>Адрес доставки в запросе оформления заказа.</summary>
public sealed record CheckoutAddressDto(
    string City,
    string? PostalCode,
    string Street,
    string House,
    string? Apartment,
    string? Entrance,
    string? Floor,
    string? Intercom);

public sealed record CheckoutRequestDto(
    string RecipientName,
    string RecipientPhone,
    string? RecipientEmail,
    CheckoutAddressDto Address,
    string DeliveryMethod,
    bool SaveAddress,
    string? Comment);

public sealed record CheckoutResultDto(string Id, int Number);

public sealed record OrderItemDto(string? ProductId, string Name, string Sku, decimal Price, int Quantity);

public sealed record OrderDto(
    string Id,
    int Number,
    string UserId,
    string Status,
    DateTimeOffset CreatedAt,
    string RecipientName,
    string RecipientPhone,
    string? RecipientEmail,
    OrderAddressDto Address,
    string? Comment,
    string? PromoCode,
    decimal Subtotal,
    decimal Discount,
    string DeliveryMethod,
    decimal Delivery,
    decimal Total,
    IReadOnlyList<OrderItemDto> Items);

/// <summary>Смена статуса заказа менеджером: {status: "new" | "confirmed" | "delivered" | "cancelled"}.</summary>
public sealed record SetOrderStatusDto(string Status);
