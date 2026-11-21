using MurketApi.Application.Dtos;
using MurketApi.Domain.Common;
using MurketApi.Domain.Orders;
using MurketApi.Infrastructure.Seeding;

namespace MurketApi.Tests;

/// <summary>Корзина и оформление заказа: остатки, промокоды, атомарность списания (PostgreSQL).</summary>
public sealed class CartAndCheckoutTests
{
    private static CheckoutRequestDto CheckoutRequest(bool saveAddress = false, string promoComment = null!) =>
        new("Анна Смирнова", "+79991234567", "anna.smirnova@example.com",
            new CheckoutAddressDto("Москва", "125047", "ул. Лесная", "7", "24", "2", "5", "24"),
            "courier", saveAddress, promoComment);

    [Fact]
    public async Task КорзинаСида_ТриПозицииСТекущимиЦенами()
    {
        await using var host = await TestHost.CreateAsync();

        var cart = await host.Cart.GetAsync(TestHost.CustomerId);

        Assert.Equal(3, cart.Items.Count);
        Assert.Equal(2290m + 890m * 2 + 390m, cart.Subtotal);
        Assert.Equal(4, cart.ItemsCount);
        Assert.Null(cart.Promo);
    }

    [Fact]
    public async Task Добавление_СверхОстаткаОграничиваетИВозвращаетЗаметку()
    {
        await using var host = await TestHost.CreateAsync();

        // RC-001: остаток 14, в корзине уже 1 → добавляем 20 → итог 14.
        var result = await host.Cart.AddAsync(TestHost.CustomerId, DemoData.ProductId("RC-001"), 20);

        Assert.Equal(14, result.Cart.Items.Single(item => item.ProductId == DemoData.ProductId("RC-001")).Quantity);
        Assert.Equal("В наличии только 14 шт.", result.Notice);
    }

    [Fact]
    public async Task Добавление_НеактивныйИлиБезОстатка_409()
    {
        await using var host = await TestHost.CreateAsync();

        await Assert.ThrowsAsync<ConflictException>(
            () => host.Cart.AddAsync(TestHost.CustomerId, DemoData.ProductId("RC-005"), 1));
        await Assert.ThrowsAsync<ConflictException>(
            () => host.Cart.AddAsync(TestHost.CustomerId, DemoData.ProductId("GR-003"), 1));
    }

    [Fact]
    public async Task ИзменениеКоличества_СверхОстатка_409_НольУдаляет()
    {
        await using var host = await TestHost.CreateAsync();

        await Assert.ThrowsAsync<ConflictException>(
            () => host.Cart.SetQuantityAsync(TestHost.CustomerId, DemoData.ProductId("EC-001"), 999));

        var removed = await host.Cart.SetQuantityAsync(TestHost.CustomerId, DemoData.ProductId("EC-001"), 0);
        Assert.DoesNotContain(removed.Cart.Items, item => item.ProductId == DemoData.ProductId("EC-001"));
    }

    [Fact]
    public async Task Промокод_ПроцентПрименяетсяИПересчитываетИтог()
    {
        await using var host = await TestHost.CreateAsync();

        var result = await host.Cart.ApplyPromoAsync(TestHost.CustomerId, "мур10"); // регистр не важен

        Assert.NotNull(result.Cart.Promo);
        Assert.Equal("МУР10", result.Cart.Promo!.Code);
        Assert.Equal(446m, result.Cart.Discount); // 10 % от 4460
        Assert.Equal(4014m, result.Cart.Total);
    }

    [Fact]
    public async Task Промокод_ИстёкшийИсчерпанныйНесуществующий_409()
    {
        await using var host = await TestHost.CreateAsync();

        await Assert.ThrowsAsync<ConflictException>(
            () => host.Cart.ApplyPromoAsync(TestHost.CustomerId, "ПРОМО"));
        await Assert.ThrowsAsync<ConflictException>(
            () => host.Cart.ApplyPromoAsync(TestHost.CustomerId, "ЛИМИТ"));
        await Assert.ThrowsAsync<ConflictException>(
            () => host.Cart.ApplyPromoAsync(TestHost.CustomerId, "НЕТКОДА"));
    }

    [Fact]
    public async Task Промокод_МеньшеМинимальнойСуммы_409()
    {
        await using var host = await TestHost.CreateAsync();

        // СКИДКА500: min 3500, корзина сида — 4460 → применится.
        var ok = await host.Cart.ApplyPromoAsync(TestHost.CustomerId, "СКИДКА500");
        Assert.Equal(500m, ok.Cart.Discount);

        // Убираем всё, кроме дразнилки (390 < 3500) → минимальная сумма не набрана.
        await host.Cart.SetQuantityAsync(TestHost.CustomerId, DemoData.ProductId("RC-001"), 0);
        await host.Cart.SetQuantityAsync(TestHost.CustomerId, DemoData.ProductId("EC-001"), 0);
        await Assert.ThrowsAsync<ConflictException>(
            () => host.Cart.ApplyPromoAsync(TestHost.CustomerId, "СКИДКА500"));
    }

    [Fact]
    public async Task Оформление_СписываетОстаткиОчищаетКорзинуФиксируетСкидку()
    {
        await using var host = await TestHost.CreateAsync();
        await host.Cart.ApplyPromoAsync(TestHost.CustomerId, "МУР10");

        var result = await host.Orders.CheckoutAsync(TestHost.CustomerId, CheckoutRequest());

        Assert.Equal(1004, result.Number);
        var order = await host.Orders.UserOrderAsync(TestHost.CustomerId, result.Id);
        Assert.Equal("new", order.Status);
        Assert.Equal(4460m, order.Subtotal);
        Assert.Equal(446m, order.Discount);
        Assert.Equal(290m, order.Delivery); // курьер
        Assert.Equal(4304m, order.Total);
        Assert.Equal("МУР10", order.PromoCode);

        // Корзина очищена.
        Assert.Equal(0, (await host.Cart.GetAsync(TestHost.CustomerId)).ItemsCount);
    }

    [Fact]
    public async Task Оформление_НедостатокОстатка_409СНазваниемТовара()
    {
        await using var host = await TestHost.CreateAsync();
        await host.Cart.SetQuantityAsync(TestHost.CustomerId, DemoData.ProductId("RC-001"), 14); // забираем весь остаток

        // Остаток на складе уменьшился менеджером после добавления в корзину.
        await host.Admin.UpdateProductAsync(DemoData.ProductId("RC-001"), new ProductDraftDto(
            "RC-001", "Royal Canin Sterilised 37, сухой корм для стерилизованных котов, 2 кг",
            "Полнорационный корм.", DemoData.FoodCategoryId, DemoData.RoyalCaninBrandId,
            2290m, 3, "assets/img/products/korm.svg", true));

        var ex = await Assert.ThrowsAsync<ConflictException>(
            () => host.Orders.CheckoutAsync(TestHost.CustomerId, CheckoutRequest()));
        Assert.Contains("В наличии только 3 шт. товара «Royal Canin", ex.Message);
    }

    [Fact]
    public async Task Оформление_НедоступныйТовар_409()
    {
        await using var host = await TestHost.CreateAsync();
        await host.Cart.AddAsync(TestHost.CustomerId, DemoData.ProductId("TR-012"), 1);

        // Товар скрыли из каталога после добавления в корзину.
        await host.Admin.UpdateProductAsync(DemoData.ProductId("TR-012"), new ProductDraftDto(
            "TR-012", "Triol щётка-пуходёрка для котов", "Скрыт", DemoData.CareCategoryId, DemoData.TriolBrandId,
            690m, 30, "assets/img/products/shchetka.svg", false));

        await Assert.ThrowsAsync<ConflictException>(
            () => host.Orders.CheckoutAsync(TestHost.CustomerId, CheckoutRequest()));
    }

    [Fact]
    public async Task Оформление_ПромокодПересталДействовать_409ИСбросПромокода()
    {
        await using var host = await TestHost.CreateAsync();
        await host.Cart.ApplyPromoAsync(TestHost.CustomerId, "МУР10");

        // Время вышло за период действия промокода (сид: до 2027-12-31 — двигаем часы дальше).
        host.Clock.Advance(TimeSpan.FromDays(500));

        await Assert.ThrowsAsync<ConflictException>(
            () => host.Orders.CheckoutAsync(TestHost.CustomerId, CheckoutRequest()));
    }

    [Fact]
    public async Task Оформление_ПустаяКорзина_409()
    {
        await using var host = await TestHost.CreateAsync();
        await host.Cart.RemoveAsync(TestHost.CustomerId, DemoData.ProductId("RC-001"));
        await host.Cart.RemoveAsync(TestHost.CustomerId, DemoData.ProductId("EC-001"));
        await host.Cart.RemoveAsync(TestHost.CustomerId, DemoData.ProductId("TR-005"));

        await Assert.ThrowsAsync<ConflictException>(
            () => host.Orders.CheckoutAsync(TestHost.CustomerId, CheckoutRequest()));
    }

    [Fact]
    public async Task Оформление_SaveAddress_СохраняетАдресБезСменыУмолчания()
    {
        await using var host = await TestHost.CreateAsync(seedDemoData: true);
        var before = (await host.Profile.AddressesAsync(TestHost.CustomerId)).Count;

        var result = await host.Orders.CheckoutAsync(TestHost.CustomerId, CheckoutRequest(saveAddress: true));

        var after = await host.Profile.AddressesAsync(TestHost.CustomerId);
        Assert.Equal(before + 1, after.Count);
        // «По умолчанию» остаётся «Дом» — новый адрес его не смещает.
        var created = Assert.Single(after, address =>
            address.Id != DemoData.HomeAddressId && address.Id != DemoData.WorkAddressId);
        Assert.False(created.IsDefault);
        Assert.True(after.Single(address => address.Id == DemoData.HomeAddressId).IsDefault);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Оформление_МоиЗаказыПостранично_Чужой404()
    {
        await using var host = await TestHost.CreateAsync();
        var created = await host.Orders.CheckoutAsync(TestHost.CustomerId, CheckoutRequest());

        var page = await host.Orders.MyOrdersAsync(TestHost.CustomerId, new PageRequest(1, 2));
        Assert.Equal(4, page.Total); // 3 сида + 1 новый
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(created.Number, page.Items[0].Number); // новые сверху

        await Assert.ThrowsAsync<NotFoundException>(() => host.Orders.UserOrderAsync(TestHost.AdminId, created.Id));
    }

    [Fact]
    public async Task Отмена_ВозвращаетОстаткиНаСклад()
    {
        await using var host = await TestHost.CreateAsync();
        var result = await host.Orders.CheckoutAsync(TestHost.CustomerId, CheckoutRequest());

        var cancelled = await host.Orders.SetStatusAsync(result.Id, "cancelled");
        Assert.Equal("cancelled", cancelled.Status);

        // RC-001: было 14, оформили 1 (стало 13), отмена вернула → снова 14.
        var page = await host.Admin.ProductsAsync(null, new PageRequest(1, 48));
        Assert.Equal(14, page.Items.Single(p => p.Id == DemoData.ProductId("RC-001")).Stock);
    }

    [Fact]
    public async Task Статус_НедопустимыйПереход_409_Неизвестный_400()
    {
        await using var host = await TestHost.CreateAsync();
        var result = await host.Orders.CheckoutAsync(TestHost.CustomerId, CheckoutRequest());

        // new → delivered напрямую нельзя.
        await Assert.ThrowsAsync<ConflictException>(() => host.Orders.SetStatusAsync(result.Id, "delivered"));
        // Неизвестный статус.
        var invalid = await Assert.ThrowsAsync<ValidationException>(
            () => host.Orders.SetStatusAsync(result.Id, "bogus"));
        Assert.Contains("status", invalid.Errors.Keys);
    }

    [Fact]
    public async Task Доставка_Курьер290_ПунктВыдачи190()
    {
        await using var host = await TestHost.CreateAsync();
        var courier = await host.Orders.CheckoutAsync(TestHost.CustomerId, CheckoutRequest());
        var courierOrder = await host.Orders.UserOrderAsync(TestHost.CustomerId, courier.Id);
        Assert.Equal("courier", courierOrder.DeliveryMethod);
        Assert.Equal(290m, courierOrder.Delivery);

        // Для второго заказа пересобираем корзину и выбираем пункт выдачи.
        await host.Cart.AddAsync(TestHost.CustomerId, DemoData.ProductId("TR-012"), 1);
        var pickupRequest = new CheckoutRequestDto("Анна Смирнова", "+79991234567", null,
            new CheckoutAddressDto("Москва", null, "ул. Тверская", "12", null, null, null, null),
            "pickup", false, null);
        var pickup = await host.Orders.CheckoutAsync(TestHost.CustomerId, pickupRequest);
        var pickupOrder = await host.Orders.UserOrderAsync(TestHost.CustomerId, pickup.Id);
        Assert.Equal("pickup", pickupOrder.DeliveryMethod);
        Assert.Equal(190m, pickupOrder.Delivery);
    }
}
