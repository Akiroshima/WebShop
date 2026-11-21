using MurketApi.Application.Abstractions;
using MurketApi.Application.Dtos;
using MurketApi.Application.Mapping;
using MurketApi.Application.Validation;
using MurketApi.Domain.Catalog;
using MurketApi.Domain.Cart;
using MurketApi.Domain.Common;
using MurketApi.Domain.Orders;
using MurketApi.Domain.Promos;
using MurketApi.Domain.Services;
using MurketApi.Domain.Users;

namespace MurketApi.Application.Services;

/// <summary>
/// Заказы: оформление с повторной валидацией остатков и промокода, «Мои заказы» (чужие — 404),
/// смена статуса менеджером с возвратом остатков при отмене.
/// </summary>
public sealed class OrderService(
    IOrderRepository orders,
    ICatalogRepository catalog,
    ICartRepository carts,
    IPromoRepository promos,
    IAddressRepository addresses,
    IUnitOfWork unitOfWork,
    IClock clock)
{

    /// <summary>Оформление заказа из корзины: снимки цен/состава/адреса, списание остатков, рост used_count.</summary>
    public CheckoutResultDto Checkout(string userId, CheckoutRequestDto request)
    {
        return unitOfWork.Execute(() =>
        {
            var errors = new ValidationRules.Errors();
            if (string.IsNullOrWhiteSpace(request.RecipientName))
            {
                errors.Add("recipientName", "Укажите имя получателя");
            }

            if (!Phone.IsValidE164(Phone.Normalize(request.RecipientPhone)))
            {
                errors.Add("recipientPhone", "Введите корректный номер телефона");
            }

            if (request.Address is null
                || string.IsNullOrWhiteSpace(request.Address.City)
                || string.IsNullOrWhiteSpace(request.Address.Street)
                || string.IsNullOrWhiteSpace(request.Address.House))
            {
                errors.Add("address", "Укажите город, улицу и дом");
            }

            if (!string.IsNullOrWhiteSpace(request.RecipientEmail) && !ValidationRules.IsValidEmail(request.RecipientEmail))
            {
                errors.Add("recipientEmail", "Введите корректный email");
            }

            var delivery = ValidationRules.TryParseDelivery(request.DeliveryMethod);
            if (delivery is null)
            {
                errors.Add("deliveryMethod", "Выберите способ доставки");
            }

            if (errors.Any)
            {
                throw errors.ToException();
            }

            var cart = carts.FindByUser(userId);
            if (cart is null)
            {
                throw new ConflictException("Корзина пуста");
            }

            var snapshot = CartCalculatorSnapshot(cart);
            if (snapshot.Lines.Count == 0)
            {
                throw new ConflictException("Корзина пуста");
            }

            // Повторная валидация остатков и доступности — по фактическим позициям корзины.
            foreach (var item in cart.Items)
            {
                var product = catalog.FindProduct(item.ProductId);
                if (product is null || !product.IsActive)
                {
                    throw new ConflictException("Некоторые товары стали недоступны");
                }

                if (item.Quantity > product.Stock)
                {
                    throw new ConflictException($"В наличии только {product.Stock} шт. товара «{product.Name}»");
                }
            }

            // Повторная валидация промокода.
            string? promoCode = null;
            decimal discount = 0m;
            if (cart.PromoCode is not null)
            {
                var promo = promos.FindByCode(cart.PromoCode);
                if (promo is null || !promo.IsUsable(clock.UtcNow) || snapshot.Subtotal < promo.MinSubtotal)
                {
                    cart.RemovePromo();
                    carts.Save(cart);
                    throw new ConflictException("Промокод больше не действует");
                }

                promoCode = promo.Code;
                discount = snapshot.Discount;
                promo.RegisterUse();
            }

            if (request.SaveAddress && addresses.CountForUser(userId) >= Address.MaxPerUser)
            {
                throw new ConflictException($"Нельзя сохранить больше {Address.MaxPerUser} адресов");
            }

            var order = Order.Place(
                id: Guid.NewGuid().ToString("N"),
                number: orders.NextNumber(),
                userId: userId,
                recipientName: request.RecipientName,
                recipientPhone: request.RecipientPhone,
                recipientEmail: request.RecipientEmail,
                address: AddressSnapshot.Create(
                    request.Address!.City.Trim(),
                    Normalize(request.Address.PostalCode),
                    request.Address.Street.Trim(),
                    request.Address.House.Trim(),
                    Normalize(request.Address.Apartment),
                    Normalize(request.Address.Entrance),
                    Normalize(request.Address.Floor),
                    Normalize(request.Address.Intercom)),
                comment: request.Comment,
                deliveryMethod: delivery!.Value,
                promoCode: promoCode,
                subtotal: snapshot.Subtotal,
                discount: discount,
                items: [.. snapshot.Lines.Select(line => OrderItem.Create(
                    line.Product.Id,
                    line.Product.Name,
                    line.Product.Sku,
                    line.Product.Price,
                    line.Quantity))],
                createdAt: clock.UtcNow);
            orders.Add(order);

            // Списание остатков — атомарно в той же единице работы.
            foreach (var line in snapshot.Lines)
            {
                catalog.FindProduct(line.Product.Id)!.Reserve(line.Quantity);
            }

            if (request.SaveAddress)
            {
                var defaultExisted = addresses.ForUser(userId).Any(existing => existing.IsDefault);
                addresses.Add(Address.Create(
                    Guid.NewGuid().ToString("N"), userId, null,
                    order.RecipientName, order.RecipientPhone,
                    order.Address.City, order.Address.Street, order.Address.House,
                    order.Address.Apartment, order.Address.Entrance, order.Address.Floor,
                    order.Address.Intercom, order.Address.PostalCode, !defaultExisted));
            }

            cart.Clear();
            carts.Save(cart);

            return new CheckoutResultDto(order.Id, order.Number);
        });
    }

    /// <summary>Заказы пользователя — постранично, новые сверху.</summary>
    public PagedResult<OrderDto> MyOrders(string userId, PageRequest page) => unitOfWork.Execute(() =>
    {
        var result = orders.ForUser(userId, page);
        return new PagedResult<OrderDto>(
            [.. result.Items.Select(order => order.ToOrderDto())], result.Total, result.Page, result.PageSize);
    });

    /// <summary>Свой заказ; чужой — 404 (существование не раскрывается).</summary>
    public OrderDto UserOrder(string userId, string orderId)
    {
        var order = orders.FindById(orderId);
        if (order is null || order.UserId != userId)
        {
            throw new NotFoundException("Заказ не найден");
        }

        return order.ToOrderDto();
    }

    /// <summary>Все заказы магазина с фильтром по статусу — постранично (модуль менеджера).</summary>
    public PagedResult<OrderDto> AdminOrders(string? status, PageRequest page) => unitOfWork.Execute(() =>
    {
        OrderStatus? parsed = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            parsed = ValidationRules.TryParseStatus(status)
                ?? throw new ValidationException("Данные заполнены неверно",
                    new Dictionary<string, string[]> { ["status"] = ["Неизвестный статус"] });
        }

        var result = orders.All(parsed, page);
        return new PagedResult<OrderDto>(
            [.. result.Items.Select(order => order.ToOrderDto())], result.Total, result.Page, result.PageSize);
    });

    /// <summary>Смена статуса менеджером; при отмене остатки возвращаются на склад.</summary>
    public OrderDto SetStatus(string orderId, string statusValue)
    {
        return unitOfWork.Execute(() =>
        {
            var next = ValidationRules.TryParseStatus(statusValue)
                ?? throw new ValidationException("Данные заполнены неверно",
                    new Dictionary<string, string[]> { ["status"] = ["Неизвестный статус"] });
            var order = orders.FindById(orderId) ?? throw new NotFoundException("Заказ не найден");

            order.TransitionTo(next);
            if (next == OrderStatus.Cancelled)
            {
                foreach (var (productId, quantity) in order.ReturnableItems())
                {
                    catalog.FindProduct(productId)?.Return(quantity);
                }
            }

            return order.ToOrderDto();
        });
    }

    private CartTotals CartCalculatorSnapshot(ShoppingCart? cart)
    {
        if (cart is null)
        {
            return new CartTotals([], 0m, 0m, 0m, null);
        }

        var products = catalog.QueryProducts(new CatalogQuery { Page = new PageRequest(1, 1_000_000) })
            .Items
            .Where(product => product.IsActive)
            .ToDictionary(product => product.Id);
        var promo = cart.PromoCode is null ? null : promos.FindByCode(cart.PromoCode);
        return CartCalculator.Calculate(cart, products, promo, clock.UtcNow);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
