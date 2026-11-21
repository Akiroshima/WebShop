using System.Net;
using System.Net.Http.Json;
using MurketApi.TestKit;

namespace MurketApi.Integration.B22;

/// <summary>B-22 · Контракт ошибок: некорректный JSON, ограничения входа, конфликты.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    [Fact]
    public async Task НекорректныйJSON_400ЕдиныйКонверт()
    {
        var api = await Api;
        using var client = api.CreateClient();

        using var content = new StringContent("{ не json", System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/v1/auth/login", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal("Данные заполнены неверно", ZoneApi.Message(body));
    }

    [Fact]
    public async Task ПятьНеудачныхВходов_429()
    {
        var api = await Api;
        using var client = api.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            var bad = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { login = "customer", password = "wrong!" });
            Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
        }

        var limited = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { login = "customer", password = "wrong!" });

        Assert.Equal((HttpStatusCode)429, limited.StatusCode);
        Assert.Equal("Слишком много попыток. Повторите позже",
            ZoneApi.Message(await ZoneApi.BodyAsync(limited)));
    }

    [Fact]
    public async Task НесуществующийТоварВКорзину_409()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var response = await client.PostAsJsonAsync("/api/v1/cart/items",
            new { productId = "00000000-0000-0000-0000-00000000dead", quantity = 1 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Товара нет в наличии", ZoneApi.Message(await ZoneApi.BodyAsync(response)));
    }

    [Fact]
    public async Task ПустойПромокод_400()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var response = await client.PostAsJsonAsync("/api/v1/cart/promo", new { code = "" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
