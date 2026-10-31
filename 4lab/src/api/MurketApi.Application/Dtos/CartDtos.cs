namespace MurketApi.Application.Dtos;

public sealed record AddCartItemDto(string ProductId, int Quantity);

public sealed record SetCartItemQuantityDto(int Quantity);

public sealed record ApplyPromoDto(string Code);

public sealed record CartItemDto(
    string ProductId,
    string Name,
    string Sku,
    string? Brand,
    decimal Price,
    int Stock,
    string ImageUrl,
    int Quantity,
    decimal Subtotal);

/// <summary>Применённый промокод: снимок правил и рассчитанная скидка.</summary>
public sealed record AppliedPromoDto(string Code, string Type, decimal Value, decimal Discount);

/// <summary>Состояние корзины с текущими ценами и итогами (считает только сервер).</summary>
public sealed record CartStateDto(
    IReadOnlyList<CartItemDto> Items,
    int ItemsCount,
    decimal Subtotal,
    AppliedPromoDto? Promo,
    decimal Discount,
    decimal Total);

/// <summary>Результат изменения корзины: новое состояние + заметка («В наличии только N шт.»).</summary>
public sealed record CartMutationResultDto(CartStateDto Cart, string? Notice = null);
