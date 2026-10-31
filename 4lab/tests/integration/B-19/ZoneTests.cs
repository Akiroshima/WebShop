using System.Net;
using System.Net.Http.Json;
using MurketApi.Infrastructure.Seeding;
using MurketApi.TestKit;

namespace MurketApi.Integration.B19;

/// <summary>B-19 · Управление каталогом (админ): список, создание, изменение, удаление товаров.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    private static object Draft(string sku, bool isActive = true) => new
    {
        sku,
        name = "Тестовый корм",
        description = "Описание тестового товара",
        categoryId = DemoData.FoodCategoryId,
        brandId = DemoData.TriolBrandId,
        price = 100m,
        stock = 5,
        imageUrl = "assets/img/products/korm.svg",
        isActive,
    };

    [Fact]
    public async Task Список_ВсеТоварыВключаяНеактивные()
    {
        var api = await Api;
        using var client = await api.LoginAsync("admin", "admin123");

        var response = await client.GetAsync("/api/v1/admin/products?pageSize=48");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal(41, body.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Создать_201_ДубликатАртикула_409()
    {
        var api = await Api;
        using var client = await api.LoginAsync("admin", "admin123");

        var created = await client.PostAsJsonAsync("/api/v1/admin/products", Draft("ZZ-999"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal("ZZ-999", ZoneApi.String(await ZoneApi.BodyAsync(created), "sku"));

        var duplicate = await client.PostAsJsonAsync("/api/v1/admin/products", Draft("zz-999"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Изменить_200()
    {
        var api = await Api;
        using var client = await api.LoginAsync("admin", "admin123");
        var created = await client.PostAsJsonAsync("/api/v1/admin/products", Draft("ZZ-998"));
        var id = ZoneApi.String(await ZoneApi.BodyAsync(created), "id");

        var response = await client.PutAsJsonAsync($"/api/v1/admin/products/{id}", Draft("ZZ-998"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal(100m, body.GetProperty("price").GetDecimal());
    }

    [Fact]
    public async Task Удалить_204_ИТоварИсчезает()
    {
        var api = await Api;
        using var client = await api.LoginAsync("admin", "admin123");
        var created = await client.PostAsJsonAsync("/api/v1/admin/products", Draft("ZZ-997"));
        var id = ZoneApi.String(await ZoneApi.BodyAsync(created), "id");

        var response = await client.DeleteAsync($"/api/v1/admin/products/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var catalog = await client.GetAsync($"/api/v1/catalog/products/{id}");
        Assert.Equal(HttpStatusCode.NotFound, catalog.StatusCode);
    }

    [Fact]
    public async Task Покупателю_403()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var response = await client.GetAsync("/api/v1/admin/products");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
