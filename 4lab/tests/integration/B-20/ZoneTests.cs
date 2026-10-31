using System.Net;
using System.Net.Http.Json;
using MurketApi.Infrastructure.Seeding;
using MurketApi.TestKit;

namespace MurketApi.Integration.B20;

/// <summary>B-20 · Управление заказами (админ): фильтр, смена статуса, возврат остатков.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    private static async Task<string> CheckoutDemoOrderAsync(HttpClient client)
    {
        var add = await client.PostAsJsonAsync("/api/v1/cart/items",
            new { productId = DemoData.ProductId("RC-001"), quantity = 1 });
        ZoneApi.EnsureSuccess(add, "наполнение корзины");
        var checkout = await client.PostAsJsonAsync("/api/v1/orders", new
        {
            recipientName = "Анна Смирнова",
            recipientPhone = "+79991234567",
            address = new { city = "Москва", street = "ул. Лесная", house = "7" },
            deliveryMethod = "courier",
        });
        ZoneApi.EnsureSuccess(checkout, "оформление заказа");
        return ZoneApi.String(await ZoneApi.BodyAsync(checkout), "id");
    }

    [Fact]
    public async Task Список_ФильтрПоСтатусуNew()
    {
        var api = await Api;
        using var client = await api.LoginAsync("admin", "admin123");

        var response = await client.GetAsync("/api/v1/admin/orders?status=new&pageSize=48");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.True(body.GetProperty("total").GetInt32() >= 1); // сид-заказ №1003
        Assert.All(body.GetProperty("items").EnumerateArray(),
            order => Assert.Equal("new", ZoneApi.String(order, "status")));
    }

    [Fact]
    public async Task НеизвестныйСтатус_400()
    {
        var api = await Api;
        using var client = await api.LoginAsync("admin", "admin123");

        var response = await client.GetAsync("/api/v1/admin/orders?status=bogus");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ПереводВПодтверждён_200()
    {
        var api = await Api;
        using var customer = await api.LoginAsync("customer", "customer123");
        using var admin = await api.LoginAsync("admin", "admin123");
        var orderId = await CheckoutDemoOrderAsync(customer);

        var response = await admin.PutAsJsonAsync($"/api/v1/admin/orders/{orderId}/status",
            new { status = "confirmed" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("confirmed", ZoneApi.String(await ZoneApi.BodyAsync(response), "status"));
    }

    [Fact]
    public async Task НедопустимыйПереход_409()
    {
        var api = await Api;
        using var customer = await api.LoginAsync("customer", "customer123");
        using var admin = await api.LoginAsync("admin", "admin123");
        var orderId = await CheckoutDemoOrderAsync(customer);

        var response = await admin.PutAsJsonAsync($"/api/v1/admin/orders/{orderId}/status",
            new { status = "delivered" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Недопустимый переход статуса заказа", ZoneApi.Message(await ZoneApi.BodyAsync(response)));
    }

    [Fact]
    public async Task Отмена_ВозвращаетОстаткиНаСклад()
    {
        var api = await Api;
        using var customer = await api.LoginAsync("customer", "customer123");
        using var admin = await api.LoginAsync("admin", "admin123");
        var before = ZoneApi.BodyAsync(await customer.GetAsync(
            $"/api/v1/catalog/products/{DemoData.ProductId("RC-001")}")).Result;
        var stockBefore = before.GetProperty("stock").GetInt32();

        var orderId = await CheckoutDemoOrderAsync(customer);
        var cancel = await admin.PutAsJsonAsync($"/api/v1/admin/orders/{orderId}/status",
            new { status = "cancelled" });
        ZoneApi.EnsureSuccess(cancel, "отмена заказа");

        var after = await ZoneApi.BodyAsync(await customer.GetAsync(
            $"/api/v1/catalog/products/{DemoData.ProductId("RC-001")}"));
        Assert.Equal(stockBefore, after.GetProperty("stock").GetInt32());
    }
}
