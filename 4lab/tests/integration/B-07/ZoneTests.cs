using System.Net;
using System.Net.Http.Json;
using MurketApi.TestKit;

namespace MurketApi.Integration.B07;

/// <summary>B-07 · Список товаров: постраничная выдача и нормализация параметров.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    [Fact]
    public async Task ПерваяСтраница_РазмерИТоталТолькоАктивных()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal(2, body.GetProperty("items").GetArrayLength());
        Assert.Equal(39, body.GetProperty("total").GetInt32());
        Assert.Equal(1, body.GetProperty("page").GetInt32());
        Assert.Equal(2, body.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task ВтораяСтраница_НеПересекаетсяСПервой()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var first = ZoneApi.BodyAsync(await client.GetAsync("/api/v1/catalog/products?page=1&pageSize=20")).Result;
        var second = ZoneApi.BodyAsync(await client.GetAsync("/api/v1/catalog/products?page=2&pageSize=20")).Result;

        var firstIds = first.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("id").GetString()).ToHashSet();
        var secondIds = second.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("id").GetString()).ToList();
        Assert.Empty(firstIds.Intersect(secondIds));
    }

    [Fact]
    public async Task НекорректнаяСтраница_400Полем()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products?page=0&pageSize=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.True(body.GetProperty("errors").TryGetProperty("page", out _));
        Assert.True(body.GetProperty("errors").TryGetProperty("pageSize", out _));
    }

    [Fact]
    public async Task УмолчаниеСтраницы_12Товаров()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products");

        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal(12, body.GetProperty("items").GetArrayLength());
        Assert.Equal(12, body.GetProperty("pageSize").GetInt32());
    }
}
