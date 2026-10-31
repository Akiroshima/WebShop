using MurketApi.Domain.Cart;
using MurketApi.Domain.Catalog;
using MurketApi.Domain.Common;
using MurketApi.Domain.Orders;
using MurketApi.Domain.Promos;
using MurketApi.Domain.Services;
using MurketApi.Domain.Users;

namespace MurketApi.Tests;

/// <summary>Юнит-тесты доменного слоя: политики, инварианты, расчёты.</summary>
public sealed class DomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("Пароль1!", true)]
    [InlineData("short1!", false)]
    [InlineData("nodigits!!", false)]
    [InlineData("1234567!", false)]
    [InlineData("nopecials1", false)]
    [InlineData("", false)]
    public void PasswordPolicy_ПроверяетПравила(string password, bool expected)
    {
        Assert.Equal(expected, PasswordPolicy.IsAcceptable(password));
    }

    [Theory]
    [InlineData("+79991234567", "+79991234567")]
    [InlineData("8 999 123-45-67", "+79991234567")]
    [InlineData("79991234567", "+79991234567")]
    [InlineData("", "")]
    public void Phone_НормализуетКE164(string input, string expected)
    {
        Assert.Equal(expected, Phone.Normalize(input));
    }

    [Fact]
    public void Phone_ОтвергаетНеРоссийскиеНомера()
    {
        Assert.False(Phone.IsValidE164(Phone.Normalize("+12345678901")));
        Assert.False(Phone.IsValidE164(Phone.Normalize("+7999123456"))); // 9 цифр
        Assert.True(Phone.IsValidE164(Phone.Normalize("+79991234567")));
    }

    [Fact]
    public void Money_ОкругляетДоКопеекОтНуля()
    {
        Assert.Equal(10.13m, Money.Round(10.125m));
        Assert.Equal(10.12m, Money.Round(10.115m)); // AwayFromZero: 10.115 → 10.12 (не банковское)
        Assert.Equal(0m, Money.Round(0.001m));
    }

    [Fact]
    public void PromoCode_ПроцентнаяСкидкаНеБольшеСуммы()
    {
        var promo = PromoCode.Create("p1", "МУР10", PromoType.Percent, 10m, 1000m,
            Now.AddDays(-1), Now.AddDays(1), null, 0, true);

        Assert.Equal(178m, promo.DiscountFor(1780m));
        Assert.Equal(50m, promo.DiscountFor(500m)); // 10 % от суммы
    }

    [Fact]
    public void PromoCode_ФиксированнаяСкидкаОграниченаСуммойТоваров()
    {
        var promo = PromoCode.Create("p1", "СКИДКА500", PromoType.Fixed, 500m, 0m,
            Now.AddDays(-1), Now.AddDays(1), null, 0, true);

        Assert.Equal(500m, promo.DiscountFor(1780m));
        Assert.Equal(120m, promo.DiscountFor(120m)); // скидка не больше суммы
    }

    [Fact]
    public void PromoCode_УсловияПрименимости_ПериодЛимитАктивность()
    {
        var expired = PromoCode.Create("p1", "ПРОМО", PromoType.Percent, 15m, 0m,
            Now.AddDays(-10), Now.AddDays(-1), null, 0, true);
        var exhausted = PromoCode.Create("p2", "ЛИМИТ", PromoType.Percent, 5m, 0m,
            Now.AddDays(-1), Now.AddDays(1), 2, 2, true);
        var inactive = PromoCode.Create("p3", "ВЫКЛ", PromoType.Percent, 5m, 0m,
            Now.AddDays(-1), Now.AddDays(1), null, 0, false);
        var ok = PromoCode.Create("p4", "ОК", PromoType.Percent, 5m, 0m,
            Now.AddDays(-1), Now.AddDays(1), 10, 9, true);

        Assert.False(expired.IsUsable(Now));
        Assert.False(exhausted.IsUsable(Now));
        Assert.False(inactive.IsUsable(Now));
        Assert.True(ok.IsUsable(Now));
    }

    [Fact]
    public void OrderStatusPolicy_ДопускаетТолькоПравильныеПереходы()
    {
        Assert.True(OrderStatusPolicy.CanTransition(OrderStatus.New, OrderStatus.Confirmed));
        Assert.True(OrderStatusPolicy.CanTransition(OrderStatus.New, OrderStatus.Cancelled));
        Assert.True(OrderStatusPolicy.CanTransition(OrderStatus.Confirmed, OrderStatus.Delivered));
        Assert.True(OrderStatusPolicy.CanTransition(OrderStatus.Confirmed, OrderStatus.Cancelled));
        Assert.False(OrderStatusPolicy.CanTransition(OrderStatus.New, OrderStatus.Delivered));
        Assert.False(OrderStatusPolicy.CanTransition(OrderStatus.Delivered, OrderStatus.Cancelled));
        Assert.False(OrderStatusPolicy.CanTransition(OrderStatus.Cancelled, OrderStatus.Confirmed));
    }

    [Fact]
    public void Order_Reserve_ОтказываетПриНедостаткеОстатка()
    {
        var product = Product.Create("p1", "SKU-1", "Товар", "", "c1", null, 100m, 3, "img", 0, false, true, Now);

        product.Reserve(3);
        Assert.Equal(0, product.Stock);

        Assert.Throws<ConflictException>(() => product.Reserve(1));
    }

    [Fact]
    public void Order_TransitionTo_ЗащищаетПереходы()
    {
        var order = PlaceOrder();

        order.TransitionTo(OrderStatus.Confirmed);
        order.TransitionTo(OrderStatus.Delivered);

        Assert.Throws<ConflictException>(() => order.TransitionTo(OrderStatus.Cancelled));
        Assert.Equal(OrderStatus.Delivered, order.Status);
    }

    [Fact]
    public void PageRequest_НормализуетГраницы()
    {
        var request = PageRequest.Normalize(null, null);
        Assert.Equal((1, 12), (request.Page, request.PageSize));

        var big = PageRequest.Normalize(0, 100);
        Assert.Equal((1, 48), (big.Page, big.PageSize));
    }

    [Fact]
    public void CartCalculator_ОтбрасываетНедоступныеТоварыИСчитаетПромокод()
    {
        var cart = ShoppingCart.Create("u1");
        cart.AddItem("p1", 2, 10, Now);
        cart.AddItem("dead", 1, 5, Now);
        cart.ApplyPromo("МУР10");

        var products = new Dictionary<string, Product>
        {
            ["p1"] = Product.Create("p1", "SKU-1", "Корм", "", "c1", null, 500m, 10, "img", 0, false, true, Now),
            ["dead"] = Product.Create("dead", "SKU-2", "Скрыт", "", "c1", null, 100m, 5, "img", 0, false, false, Now),
        };
        var promo = PromoCode.Create("p10", "МУР10", PromoType.Percent, 10m, 0m,
            Now.AddDays(-1), Now.AddDays(1), null, 0, true);

        var totals = CartCalculator.Calculate(cart, products, promo, Now);

        var line = Assert.Single(totals.Lines);
        Assert.Equal("p1", line.Product.Id);
        Assert.Equal(1000m, totals.Subtotal);
        Assert.Equal(100m, totals.Discount);
        Assert.Equal(900m, totals.Total);
    }

    private static Order PlaceOrder() =>
        Order.Place("o1", 1001, "u1", "Анна", "+79991234567", "a@b.ru",
            AddressSnapshot.Create("Москва", "123", "ул. Лесная", "7", null, null, null, null),
            null, DeliveryMethod.Courier, null, 1000m, 0m,
            [OrderItem.Create("p1", "Товар", "SKU-1", 500m, 2)], Now);
}
