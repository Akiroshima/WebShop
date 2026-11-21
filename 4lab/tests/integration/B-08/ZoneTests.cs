using System.Net;
using System.Net.Http.Json;
using MurketApi.Infrastructure.Seeding;
using MurketApi.TestKit;

namespace MurketApi.Integration.B08;

/// <summary>B-08 · Фильтры каталога: категория, цена, наличие.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    [Fact]
    public async Task ФильтрПоКатегории()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync(
            $"/api/v1/catalog/products?category={DemoData.ToysCategoryId}&pageSize=48");

        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal(7, body.GetProperty("total").GetInt32());
        Assert.All(body.GetProperty("items").EnumerateArray(),
            item => Assert.Equal(DemoData.ToysCategoryId, item.GetProperty("categoryId").GetString()));
    }

    [Fact]
    public async Task ФильтрПоЦене_ДиапазонВключительно()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products?priceMin=500&priceMax=1000&pageSize=48");

        var body = await ZoneApi.BodyAsync(response);
        Assert.All(body.GetProperty("items").EnumerateArray(), item =>
        {
            var price = item.GetProperty("price").GetDecimal();
            Assert.True(price >= 500 && price <= 1000);
        });
    }

    [Fact]
    public async Task Фильтр_МинимумБольшеМаксимума_400()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products?priceMin=2000&priceMax=1000");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.True(body.GetProperty("errors").TryGetProperty("priceMin", out _));
    }

    [Fact]
    public async Task Фильтр_ТолькоВНаличии()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products?available=true&pageSize=48");

        var body = await ZoneApi.BodyAsync(response);
        Assert.All(body.GetProperty("items").EnumerateArray(),
            item => Assert.True(item.GetProperty("stock").GetInt32() > 0));
    }
}
