using System.Net;
using System.Net.Http.Json;
using MurketApi.TestKit;

namespace MurketApi.Integration.B15;

/// <summary>B-15 · «Мои заказы»: список, деталь со снимками, границы доступа.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    [Fact]
    public async Task Список_ТриСидовыхЗаказаПостранично()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var response = await client.GetAsync("/api/v1/orders?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal(3, body.GetProperty("total").GetInt32());
        Assert.Equal(2, body.GetProperty("items").GetArrayLength());
        // Новые сверху: первый — заказ №1003 (дата 2026-10-21).
        Assert.Equal(1003, body.GetProperty("items")[0].GetProperty("number").GetInt32());
    }

    [Fact]
    public async Task Деталь_СнимкиПозицийИАдреса()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");
        var list = await ZoneApi.BodyAsync(await client.GetAsync("/api/v1/orders"));
        var orderId = list.GetProperty("items")[0].GetProperty("id").GetString()!;

        var response = await client.GetAsync($"/api/v1/orders/{orderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var order = await ZoneApi.BodyAsync(response);
        Assert.Equal("pickup", ZoneApi.String(order, "deliveryMethod"));
        Assert.Equal("Москва", order.GetProperty("address").GetProperty("city").GetString());
        Assert.Equal(2, order.GetProperty("items").GetArrayLength());
        Assert.All(order.GetProperty("items").EnumerateArray(), item =>
            Assert.False(string.IsNullOrEmpty(item.GetProperty("name").GetString())));
    }

    [Fact]
    public async Task НесуществующийЗаказ_404()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var response = await client.GetAsync("/api/v1/orders/00000000-0000-0000-0000-000000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Заказ не найден", ZoneApi.Message(await ZoneApi.BodyAsync(response)));
    }

    [Fact]
    public async Task Заказы_ТолькоДляРолиПокупателя()
    {
        var api = await Api;
        using var admin = await api.LoginAsync("admin", "admin123");

        var response = await admin.GetAsync("/api/v1/orders");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Доступ запрещён", ZoneApi.Message(await ZoneApi.BodyAsync(response)));
    }
}
