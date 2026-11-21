using System.Net;
using System.Net.Http.Json;
using MurketApi.Infrastructure.Seeding;
using MurketApi.TestKit;

namespace MurketApi.Integration.B12;

/// <summary>B-12 · Мутации корзины: добавление, количество, удаление (тесты самодостаточны).</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    private static async Task<HttpClient> CustomerAsync()
    {
        var api = await Api;
        return await api.LoginAsync("customer", "customer123");
    }

    [Fact]
    public async Task ДобавлениеНовогоТовара_ПозицияПоявляется()
    {
        using var client = await CustomerAsync();

        var response = await client.PostAsJsonAsync("/api/v1/cart/items",
            new { productId = DemoData.ProductId("TR-012"), quantity = 2 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        var line = body.GetProperty("cart").GetProperty("items").EnumerateArray().Single(item =>
            item.GetProperty("productId").GetString() == DemoData.ProductId("TR-012"));
        Assert.Equal(2, line.GetProperty("quantity").GetInt32());
    }

    [Fact]
    public async Task ДобавлениеСверхОстатка_ОграничиваетСЗаметкой()
    {
        using var client = await CustomerAsync();

        var response = await client.PostAsJsonAsync("/api/v1/cart/items",
            new { productId = DemoData.ProductId("RC-001"), quantity = 100 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        var line = body.GetProperty("cart").GetProperty("items").EnumerateArray().Single(item =>
            item.GetProperty("productId").GetString() == DemoData.ProductId("RC-001"));
        Assert.Equal(14, line.GetProperty("quantity").GetInt32());
        Assert.Equal("В наличии только 14 шт.", ZoneApi.String(body, "notice"));
    }

    [Fact]
    public async Task ИзменениеКоличества_НольУдаляетПозицию()
    {
        using var client = await CustomerAsync();
        await client.PostAsJsonAsync("/api/v1/cart/items",
            new { productId = DemoData.ProductId("EC-001"), quantity = 1 });

        var setZero = await client.PutAsJsonAsync($"/api/v1/cart/items/{DemoData.ProductId("EC-001")}",
            new { quantity = 0 });
        ZoneApi.EnsureSuccess(setZero, "обнуление позиции");

        var cart = await ZoneApi.BodyAsync(await client.GetAsync("/api/v1/cart"));
        Assert.DoesNotContain(cart.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("productId").GetString() == DemoData.ProductId("EC-001"));
    }

    [Fact]
    public async Task КоличествоСверхОстатка_409ССообщением()
    {
        using var client = await CustomerAsync();
        await client.PostAsJsonAsync("/api/v1/cart/items",
            new { productId = DemoData.ProductId("EC-001"), quantity = 1 });

        var response = await client.PutAsJsonAsync($"/api/v1/cart/items/{DemoData.ProductId("EC-001")}",
            new { quantity = 9999 });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("В наличии только", ZoneApi.Message(await ZoneApi.BodyAsync(response)));
    }

    [Fact]
    public async Task УдалениеПозиции()
    {
        using var client = await CustomerAsync();
        await client.PostAsJsonAsync("/api/v1/cart/items",
            new { productId = DemoData.ProductId("TR-005"), quantity = 1 });

        var response = await client.DeleteAsync($"/api/v1/cart/items/{DemoData.ProductId("TR-005")}");

        ZoneApi.EnsureSuccess(response, "удаление позиции");
        var cart = await ZoneApi.BodyAsync(await client.GetAsync("/api/v1/cart"));
        Assert.DoesNotContain(cart.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("productId").GetString() == DemoData.ProductId("TR-005"));
    }
}
