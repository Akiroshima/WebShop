using MurketApi.Application.Abstractions;
using MurketApi.Application.Dtos;
using MurketApi.Application.Mapping;
using MurketApi.Application.Validation;
using MurketApi.Domain.Catalog;
using MurketApi.Domain.Cart;
using MurketApi.Domain.Common;
using MurketApi.Domain.Promos;
using MurketApi.Domain.Services;

namespace MurketApi.Application.Services;

/// <summary>
/// Корзина: гостевой корзины нет, операции требуют входа. Итоги считает домен; изменения выполняются
/// в одной единице работы (при EF Core — транзакция БД).
/// </summary>
public sealed class CartService(
    ICartRepository carts,
    ICatalogRepository catalog,
    IPromoRepository promos,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public CartStateDto Get(string userId) => unitOfWork.Execute(() => BuildState(userId).State);

    /// <summary>Добавление товара: повторное — увеличивает количество; превышение остатка — ограничение с заметкой.</summary>
    public CartMutationResultDto Add(string userId, string productId, int quantity)
    {
        if (quantity < 1)
        {
            quantity = 1;
        }

        return unitOfWork.Execute(() =>
        {
            var errors = new ValidationRules.Errors();
            if (string.IsNullOrWhiteSpace(productId))
            {
                errors.Add("productId", "Укажите товар");
                throw errors.ToException();
            }

            var product = catalog.FindProduct(productId);
            if (product is null || !product.IsActive || product.Stock <= 0)
            {
                throw new ConflictException("Товара нет в наличии");
            }

            var cart = carts.FindByUser(userId) ?? ShoppingCart.Create(userId);
            var before = cart.FindItem(productId)?.Quantity ?? 0;
            cart.AddItem(productId, quantity, product.Stock, clock.UtcNow);
            carts.Save(cart);

            var after = cart.FindItem(productId)!.Quantity;
            var notice = after < before + quantity ? $"В наличии только {product.Stock} шт." : null;
            return new CartMutationResultDto(BuildState(userId).State, notice);
        });
    }

    /// <summary>Изменение количества (0 и меньше — удаление позиции).</summary>
    public CartMutationResultDto SetQuantity(string userId, string productId, int quantity)
    {
        return unitOfWork.Execute(() =>
        {
            var cart = carts.FindByUser(userId);
            if (cart?.FindItem(productId) is null)
            {
                throw new NotFoundException("Товар в корзине не найден");
            }

            if (quantity <= 0)
            {
                cart.RemoveItem(productId);
                return new CartMutationResultDto(BuildState(userId).State);
            }

            var product = catalog.FindProduct(productId)
                ?? throw new NotFoundException("Товар в корзине не найден");
            if (quantity > product.Stock)
            {
                throw new ConflictException($"В наличии только {product.Stock} шт.");
            }

            cart.SetQuantity(productId, quantity);
            return new CartMutationResultDto(BuildState(userId).State);
        });
    }

    public CartMutationResultDto Remove(string userId, string productId) => unitOfWork.Execute(() =>
    {
        carts.FindByUser(userId)?.RemoveItem(productId);
        return new CartMutationResultDto(BuildState(userId).State);
    });

    /// <summary>Применение промокода: не более одного на корзину; проверка периода, лимита и минимальной суммы.</summary>
    public CartMutationResultDto ApplyPromo(string userId, string code)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        return unitOfWork.Execute(() =>
        {
            var cart = carts.FindByUser(userId) ?? ShoppingCart.Create(userId);
            if (cart.Items.Count == 0)
            {
                throw new ConflictException("Корзина пуста — промокод применить нельзя");
            }

            var promo = promos.FindByCode(normalized)
                ?? throw new ConflictException("Промокод не найден или не действует");
            if (!promo.IsUsable(clock.UtcNow))
            {
                throw new ConflictException("Промокод не найден или не действует");
            }

            if (BuildState(userId).State.Subtotal < promo.MinSubtotal)
            {
                throw new ConflictException("Сумма заказа меньше минимальной для этого промокода");
            }

            cart.ApplyPromo(promo.Code);
            carts.Save(cart);
            return new CartMutationResultDto(BuildState(userId).State);
        });
    }

    public CartMutationResultDto RemovePromo(string userId) => unitOfWork.Execute(() =>
    {
        var cart = carts.FindByUser(userId) ?? ShoppingCart.Create(userId);
        cart.RemovePromo();
        carts.Save(cart);
        return new CartMutationResultDto(BuildState(userId).State);
    });

    /// <summary>Текущее состояние корзины: актуальные цены, действующий промокод, итоги.</summary>
    public CartSnapshot BuildState(string userId)
    {
        var cart = carts.FindByUser(userId);
        if (cart is null || cart.Items.Count == 0)
        {
            return new CartSnapshot(new CartStateDto([], 0, 0m, null, 0m, 0m),
                new CartTotals([], 0m, 0m, 0m, null), null);
        }

        var products = ActiveProducts();
        var promo = cart.PromoCode is null ? null : promos.FindByCode(cart.PromoCode);
        var totals = CartCalculator.Calculate(cart, products, promo, clock.UtcNow);
        var brandNames = catalog.Brands().ToDictionary(brand => brand.Id, brand => brand.Name);
        return new CartSnapshot(totals.ToCartState(brandNames), totals, totals.AppliedPromo);
    }

    private Dictionary<string, Product> ActiveProducts() =>
        catalog.QueryProducts(new CatalogQuery { Page = new PageRequest(1, 1_000_000) })
            .Items
            .Where(product => product.IsActive)
            .ToDictionary(product => product.Id);
}

/// <summary>Снимок корзины для использования другими сервисами (оформление заказа).</summary>
public sealed record CartSnapshot(CartStateDto State, CartTotals Totals, PromoCode? Promo);
