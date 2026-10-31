using System.Net;
using System.Net.Http.Json;
using MurketApi.TestKit;

namespace MurketApi.Integration.B14;

/// <summary>B-14 · Оформление заказа из корзины через HTTP.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    private static async Task<HttpClient> CustomerAsync()
    {
        var api = await Api;
        return await api.LoginAsync("customer", "customer123");
    }

    private static object Checkout(string method, bool saveAddress = false) => new
    {
        recipientName = "Анна Смирнова",
        recipientPhone = "+79991234567",
        recipientEmail = "anna.smirnova@example.com",
        address = new
        {
            city = "Москва",
            postalCode = "125047",
            street = "ул. Лесная",
            house = "7",
            apartment = "24",
            entrance = "2",
            floor = "5",
            intercom = "24",
        },
        deliveryMethod = method,
        saveAddress,
        comment = "Тестовый заказ",
    };

    /// <summary>Тесты зоны оформляют заказы независимо от порядка: корзина наполняется явно.</summary>
    private static async Task RefillCartAsync(HttpClient client)
    {
        var add = await client.PostAsJsonAsync("/api/v1/cart/items",
            new { productId = MurketApi.Infrastructure.Seeding.DemoData.ProductId("TR-012"), quantity = 1 });
        ZoneApi.EnsureSuccess(add, "наполнение корзины");
    }

    [Fact]
    public async Task Оформить_201_НомерИтого()
    {
        using var client = await CustomerAsync();
        await RefillCartAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/orders", Checkout("courier"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.True(body.GetProperty("number").GetInt32() >= 1004);
        Assert.NotEmpty(ZoneApi.String(body, "id"));
    }

    [Fact]
    public async Task ПовторноеОформление_ПустаяКорзина_409()
    {
        using var client = await CustomerAsync();
        await RefillCartAsync(client);
        var first = await client.PostAsJsonAsync("/api/v1/orders", Checkout("pickup"));
        ZoneApi.EnsureSuccess(first, "первое оформление");

        var second = await client.PostAsJsonAsync("/api/v1/orders", Checkout("pickup"));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("Корзина пуста", ZoneApi.Message(await ZoneApi.BodyAsync(second)));
    }

    [Fact]
    public async Task НекорректныйАдрес_400Полем()
    {
        using var client = await CustomerAsync();
        var request = new
        {
            recipientName = "Анна",
            recipientPhone = "+79991234567",
            address = new { city = "", street = "", house = "" },
            deliveryMethod = "courier",
        };

        var response = await client.PostAsJsonAsync("/api/v1/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.True(body.GetProperty("errors").TryGetProperty("address", out _));
    }

    [Fact]
    public async Task ОформлениеСПромокодом_СкидкаФиксируется()
    {
        using var client = await CustomerAsync();
        await RefillCartAsync(client);

        // Корзина наполняется явно; фактическую сумму читаем из ответа применения промокода.
        var apply = await client.PostAsJsonAsync("/api/v1/cart/promo", new { code = "МУР10" });
        ZoneApi.EnsureSuccess(apply, "применение промокода");
        var cart = (await ZoneApi.BodyAsync(apply)).GetProperty("cart");
        var subtotal = cart.GetProperty("subtotal").GetDecimal();
        Assert.True(subtotal >= 1000); // минимальная сумма МУР10 набрана
        var expectedDiscount = Math.Round(subtotal * 0.10m, 2, MidpointRounding.AwayFromZero);

        var response = await client.PostAsJsonAsync("/api/v1/orders", Checkout("courier"));

        ZoneApi.EnsureSuccess(response, "оформление с промокодом");
        var orderId = ZoneApi.String(await ZoneApi.BodyAsync(response), "id");

        var order = await ZoneApi.BodyAsync(await client.GetAsync($"/api/v1/orders/{orderId}"));
        Assert.Equal("МУР10", ZoneApi.String(order, "promoCode"));
        Assert.Equal(subtotal, order.GetProperty("subtotal").GetDecimal());
        Assert.Equal(expectedDiscount, order.GetProperty("discount").GetDecimal());
        Assert.Equal(subtotal - expectedDiscount + 290m, order.GetProperty("total").GetDecimal());
    }
}
