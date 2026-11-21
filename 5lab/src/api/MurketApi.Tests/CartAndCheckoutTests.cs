using MurketApi.Application.Dtos;
using MurketApi.Domain.Common;
using MurketApi.Domain.Orders;

namespace MurketApi.Tests;

/// <summary>Корзина и оформление заказа: остатки, промокоды, атомарность списания.</summary>
public sealed class CartAndCheckoutTests
{
    private static CheckoutRequestDto CheckoutRequest(bool saveAddress = false, string promoComment = null!) =>
        new("Анна Смирнова", "+79991234567", "anna.smirnova@example.com",
            new CheckoutAddressDto("Москва", "125047", "ул. Лесная", "7", "24", "2", "5", "24"),
            "courier", saveAddress, promoComment);

    [Fact]
    public void КорзинаСида_ТриПозицииСТекущимиЦенами()
    {
        var host = new TestHost();

        var cart = host.Cart.Get(TestHost.CustomerId);

        Assert.Equal(3, cart.Items.Count);
        Assert.Equal(2290m + 890m * 2 + 390m, cart.Subtotal);
        Assert.Equal(4, cart.ItemsCount);
        Assert.Null(cart.Promo);
    }

    [Fact]
    public void Добавление_СверхОстаткаОграничиваетИВозвращаетЗаметку()
    {
        var host = new TestHost();

        // RC-001: остаток 14, в корзине уже 1 → добавляем 20 → итог 14.
        var result = host.Cart.Add(TestHost.CustomerId, "p-rc-sterilised", 20);

        Assert.Equal(14, result.Cart.Items.Single(item => item.ProductId == "p-rc-sterilised").Quantity);
        Assert.Equal("В наличии только 14 шт.", result.Notice);
    }

    [Fact]
    public void Добавление_НеактивныйИлиБезОстатка_409()
    {
        var host = new TestHost();

        Assert.Throws<ConflictException>(() => host.Cart.Add(TestHost.CustomerId, "p-rc-light", 1));
        Assert.Throws<ConflictException>(() => host.Cart.Add(TestHost.CustomerId, "p-gr-canned", 1));
    }

    [Fact]
    public void ИзменениеКоличества_СверхОстатка_409_НольУдаляет()
    {
        var host = new TestHost();

        Assert.Throws<ConflictException>(
            () => host.Cart.SetQuantity(TestHost.CustomerId, "p-ec-extra", 999));

        var removed = host.Cart.SetQuantity(TestHost.CustomerId, "p-ec-extra", 0);
        Assert.DoesNotContain(removed.Cart.Items, item => item.ProductId == "p-ec-extra");
    }

    [Fact]
    public void Промокод_ПроцентПрименяетсяИПересчитываетИтог()
    {
        var host = new TestHost();

        var result = host.Cart.ApplyPromo(TestHost.CustomerId, "мур10"); // регистр не важен

        Assert.NotNull(result.Cart.Promo);
        Assert.Equal("МУР10", result.Cart.Promo!.Code);
        Assert.Equal(446m, result.Cart.Discount); // 10 % от 4460
        Assert.Equal(4014m, result.Cart.Total);
    }

    [Fact]
    public void Промокод_ИстёкшийИсчерпанныйНесуществующий_409()
    {
        var host = new TestHost();

        Assert.Throws<ConflictException>(() => host.Cart.ApplyPromo(TestHost.CustomerId, "ПРОМО"));
        Assert.Throws<ConflictException>(() => host.Cart.ApplyPromo(TestHost.CustomerId, "ЛИМИТ"));
        Assert.Throws<ConflictException>(() => host.Cart.ApplyPromo(TestHost.CustomerId, "НЕТКОДА"));
    }

    [Fact]
    public void Промокод_МеньшеМинимальнойСуммы_409()
    {
        var host = new TestHost();

        // СКИДКА500: min 3500, корзина сида — 4360 → применится.
        var ok = host.Cart.ApplyPromo(TestHost.CustomerId, "СКИДКА500");
        Assert.Equal(500m, ok.Cart.Discount);

        // Убираем всё, кроме дразнилки (390 < 3500) → минимальная сумма не набрана.
        host.Cart.SetQuantity(TestHost.CustomerId, "p-rc-sterilised", 0);
        host.Cart.SetQuantity(TestHost.CustomerId, "p-ec-extra", 0);
        Assert.Throws<ConflictException>(() => host.Cart.ApplyPromo(TestHost.CustomerId, "СКИДКА500"));
    }

    [Fact]
    public void Оформление_СписываетОстаткиОчищаетКорзинуФиксируетСкидку()
    {
        var host = new TestHost();
        host.Cart.ApplyPromo(TestHost.CustomerId, "МУР10");

        var result = host.Orders.Checkout(TestHost.CustomerId, CheckoutRequest());

        Assert.Equal(1004, result.Number);
        var order = host.Orders.UserOrder(TestHost.CustomerId, result.Id);
        Assert.Equal("new", order.Status);
        Assert.Equal(4460m, order.Subtotal);
        Assert.Equal(446m, order.Discount);
        Assert.Equal(290m, order.Delivery); // курьер
        Assert.Equal(4304m, order.Total);
        Assert.Equal("МУР10", order.PromoCode);

        // Корзина очищена.
        Assert.Equal(0, host.Cart.Get(TestHost.CustomerId).ItemsCount);
    }

    [Fact]
    public void Оформление_НедостатокОстатка_409СНазваниемТовара()
    {
        var host = new TestHost();
        host.Cart.SetQuantity(TestHost.CustomerId, "p-rc-sterilised", 14); // забираем весь остаток

        // Остаток на складе уменьшился менеджером после добавления в корзину.
        host.Admin.UpdateProduct("p-rc-sterilised", new ProductDraftDto(
            "RC-001", "Royal Canin Sterilised 37, сухой корм для стерилизованных котов, 2 кг",
            "Полнорационный корм.", "c-food", "b-royal-canin",
            2290m, 3, "assets/img/products/korm.svg", true));

        var ex = Assert.Throws<ConflictException>(() => host.Orders.Checkout(TestHost.CustomerId, CheckoutRequest()));
        Assert.Contains("В наличии только 3 шт. товара «Royal Canin", ex.Message);
    }

    [Fact]
    public void Оформление_НедоступныйТовар_409()
    {
        var host = new TestHost();
        var cart = host.Cart.Add(TestHost.CustomerId, "p-tr-brush", 1);

        // Товар скрыли из каталога после добавления в корзину.
        host.Admin.UpdateProduct("p-tr-brush", new ProductDraftDto(
            "TR-012", "Triol щётка-пуходёрка для котов", "Скрыт", "c-care", "b-triol",
            690m, 30, "assets/img/products/shchetka.svg", false));

        Assert.Throws<ConflictException>(() => host.Orders.Checkout(TestHost.CustomerId, CheckoutRequest()));
    }

    [Fact]
    public void Оформление_ПромокодПересталДействовать_409ИСбросПромокода()
    {
        var host = new TestHost();
        host.Cart.ApplyPromo(TestHost.CustomerId, "МУР10");

        // Время вышло за период действия промокода (сид: до 2027-12-31 — двигаем часы дальше).
        host.Clock.Advance(TimeSpan.FromDays(500));

        Assert.Throws<ConflictException>(() => host.Orders.Checkout(TestHost.CustomerId, CheckoutRequest()));
    }

    [Fact]
    public void Оформление_ПустаяКорзина_409()
    {
        var host = new TestHost();
        host.Cart.Remove(TestHost.CustomerId, "p-rc-sterilised");
        host.Cart.Remove(TestHost.CustomerId, "p-ec-extra");
        host.Cart.Remove(TestHost.CustomerId, "p-tr-feather");

        Assert.Throws<ConflictException>(() => host.Orders.Checkout(TestHost.CustomerId, CheckoutRequest()));
    }

    [Fact]
    public void Оформление_SaveAddress_СохраняетАдресБезСменыУмолчания()
    {
        var host = new TestHost(seedDemoData: true);
        var before = host.Profile.Addresses(TestHost.CustomerId).Count;

        var result = host.Orders.Checkout(TestHost.CustomerId, CheckoutRequest(saveAddress: true));

        var after = host.Profile.Addresses(TestHost.CustomerId);
        Assert.Equal(before + 1, after.Count);
        // «По умолчанию» остаётся a-home — новый адрес его не смещает.
        var created = Assert.Single(after, address => address.Id != "a-home" && address.Id != "a-work");
        Assert.False(created.IsDefault);
        Assert.True(after.Single(address => address.Id == "a-home").IsDefault);
        Assert.NotNull(result);
    }

    [Fact]
    public void Оформление_МоиЗаказыПостранично_Чужой404()
    {
        var host = new TestHost();
        var created = host.Orders.Checkout(TestHost.CustomerId, CheckoutRequest());

        var page = host.Orders.MyOrders(TestHost.CustomerId, new PageRequest(1, 2));
        Assert.Equal(4, page.Total); // 3 сида + 1 новый
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(created.Number, page.Items[0].Number); // новые сверху

        Assert.Throws<NotFoundException>(() => host.Orders.UserOrder(TestHost.AdminId, created.Id));
    }

    [Fact]
    public void Отмена_ВозвращаетОстаткиНаСклад()
    {
        var host = new TestHost();
        var result = host.Orders.Checkout(TestHost.CustomerId, CheckoutRequest());

        var cancelled = host.Orders.SetStatus(result.Id, "cancelled");
        Assert.Equal("cancelled", cancelled.Status);

        // RC-001: было 14, оформили 1 (стало 13), отмена вернула → снова 14.
        var product = host.Admin.Products(null, new PageRequest(1, 48)).Items.Single(p => p.Id == "p-rc-sterilised");
        Assert.Equal(14, product.Stock);
    }

    [Fact]
    public void Статус_НедопустимыйПереход_409_Неизвестный_400()
    {
        var host = new TestHost();
        var result = host.Orders.Checkout(TestHost.CustomerId, CheckoutRequest());

        // new → delivered напрямую нельзя.
        Assert.Throws<ConflictException>(() => host.Orders.SetStatus(result.Id, "delivered"));
        // Неизвестный статус.
        var invalid = Assert.Throws<ValidationException>(() => host.Orders.SetStatus(result.Id, "bogus"));
        Assert.Contains("status", invalid.Errors.Keys);
    }

    [Fact]
    public void Доставка_Курьер290_ПунктВыдачи190()
    {
        var host = new TestHost();
        var courier = host.Orders.Checkout(TestHost.CustomerId, CheckoutRequest());
        var courierOrder = host.Orders.UserOrder(TestHost.CustomerId, courier.Id);
        Assert.Equal("courier", courierOrder.DeliveryMethod);
        Assert.Equal(290m, courierOrder.Delivery);

        // Для второго заказа пересобираем корзину и выбираем пункт выдачи.
        host.Cart.Add(TestHost.CustomerId, "p-tr-brush", 1);
        var pickupRequest = new CheckoutRequestDto("Анна Смирнова", "+79991234567", null,
            new CheckoutAddressDto("Москва", null, "ул. Тверская", "12", null, null, null, null),
            "pickup", false, null);
        var pickup = host.Orders.Checkout(TestHost.CustomerId, pickupRequest);
        var pickupOrder = host.Orders.UserOrder(TestHost.CustomerId, pickup.Id);
        Assert.Equal("pickup", pickupOrder.DeliveryMethod);
        Assert.Equal(190m, pickupOrder.Delivery);
    }
}
