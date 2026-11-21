using System.Net;
using System.Net.Http.Json;
using MurketApi.Infrastructure.Seeding;
using MurketApi.TestKit;

namespace MurketApi.Integration.B10;

/// <summary>B-10 · Карточка товара: активные, неактивные, отсутствующие.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    [Fact]
    public async Task КарточкаАктивногоТовара_ВСправочнымиИменами()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync($"/api/v1/catalog/products/{DemoData.ProductId("RC-001")}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal("RC-001", ZoneApi.String(body, "sku"));
        Assert.Equal("Корм и лакомства", ZoneApi.String(body, "category"));
        Assert.Equal("Royal Canin", ZoneApi.String(body, "brand"));
        Assert.Equal(2290m, body.GetProperty("price").GetDecimal());
    }

    [Fact]
    public async Task КарточкаНеактивного_404()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync($"/api/v1/catalog/products/{DemoData.ProductId("RC-005")}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task КарточкаНесуществующего_404()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products/not-a-uuid");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Товар не найден", ZoneApi.Message(await ZoneApi.BodyAsync(response)));
    }
}
