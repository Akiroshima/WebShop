using System.Net;
using System.Net.Http.Json;
using MurketApi.TestKit;

namespace MurketApi.Integration.B11;

/// <summary>B-11 · Корзина: доступ только после входа, состояние сида.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    [Fact]
    public async Task БезВхода_401()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/cart");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Требуется вход в аккаунт", ZoneApi.Message(await ZoneApi.BodyAsync(response)));
    }

    [Fact]
    public async Task КорзинаСида_ТриПозицииСТекущимиЦенами()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var response = await client.GetAsync("/api/v1/cart");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal(3, body.GetProperty("items").GetArrayLength());
        Assert.Equal(4, body.GetProperty("itemsCount").GetInt32());
        Assert.Equal(4460m, body.GetProperty("subtotal").GetDecimal());
        Assert.Equal(4460m, body.GetProperty("total").GetDecimal());
        Assert.Null(body.GetProperty("promo").GetString());
    }

    [Fact]
    public async Task КорзинаЦены_ВсегдаАктуальныеИзКаталога()
    {
        var api = await Api;
        using var admin = await api.LoginAsync("admin", "admin123");
        using var customer = await api.LoginAsync("customer", "customer123");

        // Менеджер меняет цену товара из корзины.
        var change = await admin.PutAsJsonAsync($"/api/v1/admin/products/{MurketApi.Infrastructure.Seeding.DemoData.ProductId("RC-001")}",
            new
            {
                sku = "RC-001",
                name = "Royal Canin Sterilised 37, сухой корм для стерилизованных котов, 2 кг",
                description = "Полнорационный корм.",
                categoryId = MurketApi.Infrastructure.Seeding.DemoData.FoodCategoryId,
                brandId = MurketApi.Infrastructure.Seeding.DemoData.RoyalCaninBrandId,
                price = 1500m,
                stock = 14,
                imageUrl = "assets/img/products/korm.svg",
                isActive = true,
            });
        ZoneApi.EnsureSuccess(change, "изменение цены товара");

        try
        {
            var cart = await ZoneApi.BodyAsync(await customer.GetAsync("/api/v1/cart"));
            var line = cart.GetProperty("items").EnumerateArray().Single(item =>
                item.GetProperty("sku").GetString() == "RC-001");
            Assert.Equal(1500m, line.GetProperty("price").GetDecimal());
        }
        finally
        {
            // Возвращаем демо-цену: другие тесты зоны читают итоги корзины сида.
            var restore = await admin.PutAsJsonAsync($"/api/v1/admin/products/{MurketApi.Infrastructure.Seeding.DemoData.ProductId("RC-001")}",
                new
                {
                    sku = "RC-001",
                    name = "Royal Canin Sterilised 37, сухой корм для стерилизованных котов, 2 кг",
                    description = "Полнорационный корм.",
                    categoryId = MurketApi.Infrastructure.Seeding.DemoData.FoodCategoryId,
                    brandId = MurketApi.Infrastructure.Seeding.DemoData.RoyalCaninBrandId,
                    price = 2290m,
                    stock = 14,
                    imageUrl = "assets/img/products/korm.svg",
                    isActive = true,
                });
            ZoneApi.EnsureSuccess(restore, "возврат демо-цены");
        }
    }
}
