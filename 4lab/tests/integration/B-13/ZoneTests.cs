using System.Net;
using System.Net.Http.Json;
using MurketApi.TestKit;

namespace MurketApi.Integration.B13;

/// <summary>B-13 · Промокоды корзины: применение, отказы, снятие.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    [Fact]
    public async Task ПрименитьМУР10_СкидкаПересчитываетИтог()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var response = await client.PostAsJsonAsync("/api/v1/cart/promo",
            new { code = "мур10" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        var cart = body.GetProperty("cart");
        Assert.Equal("МУР10", cart.GetProperty("promo").GetProperty("code").GetString());
        Assert.Equal(446m, cart.GetProperty("discount").GetDecimal());
        Assert.Equal(4014m, cart.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task ИстёкшийПромокод_409()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var response = await client.PostAsJsonAsync("/api/v1/cart/promo", new { code = "ПРОМО" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task НесуществующийПромокод_409()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var response = await client.PostAsJsonAsync("/api/v1/cart/promo", new { code = "НЕТКОДА" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Промокод не найден или не действует", ZoneApi.Message(await ZoneApi.BodyAsync(response)));
    }

    [Fact]
    public async Task СнятьПромокод()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");
        await client.PostAsJsonAsync("/api/v1/cart/promo", new { code = "МУР10" });

        var response = await client.DeleteAsync("/api/v1/cart/promo");

        ZoneApi.EnsureSuccess(response, "снятие промокода");
        var cart = (await ZoneApi.BodyAsync(response)).GetProperty("cart");
        Assert.Null(cart.GetProperty("promo").GetString());
        Assert.Equal(0m, cart.GetProperty("discount").GetDecimal());
    }
}
