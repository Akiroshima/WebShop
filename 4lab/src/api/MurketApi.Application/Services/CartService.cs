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
/// в одной единице работы — транзакции БД (списание остатков при оформлении происходит рядом).
/// </summary>
public sealed class CartService(
    ICartRepository carts,
    ICatalogRepository catalog,
    IPromoRepository promos,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public Task<CartStateDto> GetAsync(string userId) =>
        unitOfWork.ExecuteAsync(async () => (await BuildStateAsync(userId)).State);

    /// <summary>Добавление товара: повторное — увеличивает количество; превышение остатка — ограничение с заметкой.</summary>
    public Task<CartMutationResultDto> AddAsync(string userId, string productId, int quantity)
    {
        if (quantity < 1)
        {
            quantity = 1;
        }

        return unitOfWork.ExecuteAsync(async () =>
        {
            var errors = new ValidationRules.Errors();
            if (string.IsNullOrWhiteSpace(productId))
            {
                errors.Add("productId", "Укажите товар");
                throw errors.ToException();
            }

            var product = await catalog.FindProductAsync(productId);
            if (product is null || !product.IsActive || product.Stock <= 0)
            {
                throw new ConflictException("Товара нет в наличии");
            }

            var cart = await carts.FindByUserAsync(userId) ?? ShoppingCart.Create(userId);
            var before = cart.FindItem(productId)?.Quantity ?? 0;
            cart.AddItem(productId, quantity, product.Stock, clock.UtcNow);
            await carts.SaveAsync(cart);

            var after = cart.FindItem(productId)!.Quantity;
            var notice = after < before + quantity ? $"В наличии только {product.Stock} шт." : null;
            return new CartMutationResultDto((await BuildStateAsync(userId)).State, notice);
        });
    }

    /// <summary>Изменение количества (0 и меньше — удаление позиции).</summary>
    public Task<CartMutationResultDto> SetQuantityAsync(string userId, string productId, int quantity)
    {
        return unitOfWork.ExecuteAsync(async () =>
        {
            var cart = await carts.FindByUserAsync(userId);
            if (cart?.FindItem(productId) is null)
            {
                throw new NotFoundException("Товар в корзине не найден");
            }

            if (quantity <= 0)
            {
                cart.RemoveItem(productId);
                return new CartMutationResultDto((await BuildStateAsync(userId)).State);
            }

            var product = await catalog.FindProductAsync(productId)
                ?? throw new NotFoundException("Товар в корзине не найден");
            if (quantity > product.Stock)
            {
                throw new ConflictException($"В наличии только {product.Stock} шт.");
            }

            cart.SetQuantity(productId, quantity);
            await carts.SaveAsync(cart);
            return new CartMutationResultDto((await BuildStateAsync(userId)).State);
        });
    }

    public Task<CartMutationResultDto> RemoveAsync(string userId, string productId) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var cart = await carts.FindByUserAsync(userId);
            if (cart is not null)
            {
                cart.RemoveItem(productId);
                await carts.SaveAsync(cart);
            }

            return new CartMutationResultDto((await BuildStateAsync(userId)).State);
        });

    /// <summary>Применение промокода: не более одного на корзину; проверка периода, лимита и минимальной суммы.</summary>
    public Task<CartMutationResultDto> ApplyPromoAsync(string userId, string code)
    {
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        return unitOfWork.ExecuteAsync(async () =>
        {
            var cart = await carts.FindByUserAsync(userId) ?? ShoppingCart.Create(userId);
            if (cart.Items.Count == 0)
            {
                throw new ConflictException("Корзина пуста — промокод применить нельзя");
            }

            var promo = await promos.FindByCodeAsync(normalized)
                ?? throw new ConflictException("Промокод не найден или не действует");
            if (!promo.IsUsable(clock.UtcNow))
            {
                throw new ConflictException("Промокод не найден или не действует");
            }

            if ((await BuildStateAsync(userId)).State.Subtotal < promo.MinSubtotal)
            {
                throw new ConflictException("Сумма заказа меньше минимальной для этого промокода");
            }

            cart.ApplyPromo(promo.Code);
            await carts.SaveAsync(cart);
            return new CartMutationResultDto((await BuildStateAsync(userId)).State);
        });
    }

    public Task<CartMutationResultDto> RemovePromoAsync(string userId) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var cart = await carts.FindByUserAsync(userId) ?? ShoppingCart.Create(userId);
            cart.RemovePromo();
            await carts.SaveAsync(cart);
            return new CartMutationResultDto((await BuildStateAsync(userId)).State);
        });

    /// <summary>Текущее состояние корзины: актуальные цены, действующий промокод, итоги.</summary>
    public async Task<CartSnapshot> BuildStateAsync(string userId)
    {
        var cart = await carts.FindByUserAsync(userId);
        if (cart is null || cart.Items.Count == 0)
        {
            return new CartSnapshot(new CartStateDto([], 0, 0m, null, 0m, 0m),
                new CartTotals([], 0m, 0m, 0m, null), null);
        }

        var products = await ActiveProductsAsync();
        var promo = cart.PromoCode is null ? null : await promos.FindByCodeAsync(cart.PromoCode);
        var totals = CartCalculator.Calculate(cart, products, promo, clock.UtcNow);
        var brandNames = (await catalog.BrandsAsync()).ToDictionary(brand => brand.Id, brand => brand.Name);
        return new CartSnapshot(totals.ToCartState(brandNames), totals, totals.AppliedPromo);
    }

    private async Task<Dictionary<string, Product>> ActiveProductsAsync()
    {
        var items = (await catalog.QueryProductsAsync(new CatalogQuery { Page = new PageRequest(1, 1_000_000) }))
            .Items
            .Where(product => product.IsActive);
        return items.ToDictionary(product => product.Id);
    }
}

/// <summary>Снимок корзины для использования другими сервисами (оформление заказа).</summary>
public sealed record CartSnapshot(CartStateDto State, CartTotals Totals, PromoCode? Promo);
