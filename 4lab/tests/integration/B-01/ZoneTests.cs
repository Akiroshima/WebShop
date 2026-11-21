using System.Net;
using System.Net.Http.Json;
using MurketApi.TestKit;

namespace MurketApi.Integration.B01;

/// <summary>B-01 · Служебные маршруты, SPA-fallback и единый конверт ошибок.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    [Fact]
    public async Task Health_ОтвечаетOk()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal("ok", ZoneApi.String(body, "status"));
    }

    [Fact]
    public async Task НезанятыйМаршрут_ОтдаётСобранныйКлиент()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/some/spa/route");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("html", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task НесуществующийТовар_404ЕдиныйКонверт()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products/00000000-0000-0000-0000-000000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal("Товар не найден", ZoneApi.Message(body));
    }

    [Fact]
    public async Task НекорректныйQuery_400СОшибкойПоля()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products?priceMin=abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal("Данные заполнены неверно", ZoneApi.Message(body));
        Assert.True(body.GetProperty("errors").TryGetProperty("priceMin", out _));
    }
}
