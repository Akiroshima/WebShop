using MurketApi.Domain.Common;

namespace MurketApi.Domain.Cart;

/// <summary>Позиция корзины: ссылка на товар и количество (цены всегда текущие, из каталога).</summary>
public sealed class CartItem
{
    public string ProductId { get; internal set; } = string.Empty;

    public int Quantity { get; internal set; }

    public DateTimeOffset AddedAt { get; internal set; }
}

/// <summary>Корзина пользователя (одна на аккаунт; гостевой корзины в системе нет).</summary>
public sealed class ShoppingCart
{
    public string UserId { get; private set; } = string.Empty;

    /// <summary>Код применённого промокода (нормализован к верхнему регистру) или null.</summary>
    public string? PromoCode { get; private set; }

    public List<CartItem> Items { get; private set; } = [];

    private ShoppingCart()
    {
    }

    public static ShoppingCart Create(string userId) => new() { UserId = userId };

    public CartItem? FindItem(string productId) => Items.FirstOrDefault(item => item.ProductId == productId);

    /// <summary>Добавляет товар или увеличивает количество существующей позиции.</summary>
    public CartItem AddItem(string productId, int quantity, int stock, DateTimeOffset now)
    {
        if (quantity < 1)
        {
            quantity = 1;
        }

        var existing = FindItem(productId);
        var target = (existing?.Quantity ?? 0) + quantity;
        if (target > stock)
        {
            target = stock;
        }

        if (target < 1)
        {
            throw new ConflictException("Товара нет в наличии");
        }

        if (existing is null)
        {
            existing = new CartItem { ProductId = productId, Quantity = target, AddedAt = now };
            Items.Add(existing);
        }
        else
        {
            existing.Quantity = target;
            existing.AddedAt = now;
        }

        return existing;
    }

    /// <summary>Устанавливает количество (0 и меньше — удаляет позицию); лимит остатка проверяет прикладной слой.</summary>
    public void SetQuantity(string productId, int quantity)
    {
        var item = FindItem(productId) ?? throw new NotFoundException("Товар в корзине не найден");
        if (quantity <= 0)
        {
            Items.Remove(item);
            return;
        }

        item.Quantity = quantity;
    }

    public void RemoveItem(string productId) => Items.RemoveAll(item => item.ProductId == productId);

    public void Clear()
    {
        Items.Clear();
        PromoCode = null;
    }

    public void ApplyPromo(string code) => PromoCode = code.Trim().ToUpperInvariant();

    public void RemovePromo() => PromoCode = null;
}
