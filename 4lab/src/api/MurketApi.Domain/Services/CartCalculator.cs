using MurketApi.Domain.Cart;
using MurketApi.Domain.Catalog;
using MurketApi.Domain.Common;
using MurketApi.Domain.Promos;

namespace MurketApi.Domain.Services;

/// <summary>Позиция корзины, сопоставленная с текущим состоянием товара каталога.</summary>
public sealed record CartLine(Product Product, int Quantity)
{
    public decimal LineSubtotal => Money.Round(Product.Price * Quantity);
}

/// <summary>Итоги корзины: считает только домен (клиенту нельзя доверять).</summary>
public sealed record CartTotals(
    IReadOnlyList<CartLine> Lines,
    decimal Subtotal,
    decimal Discount,
    decimal Total,
    PromoCode? AppliedPromo);

/// <summary>Доменный расчёт корзины: актуальные цены; скрытые и удалённые товары не показываются.</summary>
public static class CartCalculator
{
    public static CartTotals Calculate(ShoppingCart cart, IReadOnlyDictionary<string, Product> products,
        PromoCode? promo, DateTimeOffset now)
    {
        var lines = cart.Items
            .OrderBy(item => item.AddedAt)
            .Select(item => products.GetValueOrDefault(item.ProductId) is { IsActive: true } product
                ? new CartLine(product, item.Quantity)
                : null)
            .Where(line => line is not null)
            .Select(line => line!)
            .ToList();

        var subtotal = Money.Round(lines.Sum(line => line.LineSubtotal));
        if (lines.Count == 0)
        {
            return new CartTotals([], 0m, 0m, 0m, null);
        }

        var applied = promo is not null && cart.PromoCode == promo.Code && promo.IsUsable(now)
            && subtotal >= promo.MinSubtotal
                ? promo
                : null;
        var discount = applied?.DiscountFor(subtotal) ?? 0m;

        return new CartTotals(lines, subtotal, discount, Money.Round(subtotal - discount), applied);
    }
}
