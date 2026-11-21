using System.Net;
using System.Net.Http.Json;
using MurketApi.TestKit;

namespace MurketApi.Integration.B09;

/// <summary>B-09 · Поиск (название/артикул/бренд) и сортировка списка товаров.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    [Fact]
    public async Task ПоискПоАртикулу_ОдиночноеСовпадение()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products?search=rc-001&pageSize=48");

        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal(1, body.GetProperty("total").GetInt32());
        Assert.Equal("RC-001", body.GetProperty("items")[0].GetProperty("sku").GetString());
    }

    [Fact]
    public async Task ПоискПоБренду()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products?search=grandorf&pageSize=48");

        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal(3, body.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task СортировкаПоЦенеВозрастанию()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products?sort=price-asc&pageSize=48");

        var body = await ZoneApi.BodyAsync(response);
        var prices = body.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("price").GetDecimal()).ToList();
        Assert.Equal(prices.OrderBy(price => price), prices);
    }

    [Fact]
    public async Task НеизвестнаяСортировка_400()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products?sort=bogus");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.True(body.GetProperty("errors").TryGetProperty("sort", out _));
    }
}
