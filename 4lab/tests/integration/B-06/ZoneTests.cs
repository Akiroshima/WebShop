using System.Net;
using System.Net.Http.Json;
using MurketApi.Infrastructure.Seeding;
using MurketApi.TestKit;

namespace MurketApi.Integration.B06;

/// <summary>B-06 · Каталог: категории и бренды с количеством товаров (эндпоинты отдают массивы).</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    [Fact]
    public async Task Категории_ПятьСКоличествамиАктивных()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await ZoneApi.BodyAsync(response);
        Assert.Equal(5, items.GetArrayLength());
        var food = items.EnumerateArray().Single(category =>
            category.GetProperty("id").GetString() == DemoData.FoodCategoryId);
        Assert.Equal(12, food.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Бренды_ВСкоупеКатегорииТолькоСТоварами()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync($"/api/v1/catalog/brands?category={DemoData.ToysCategoryId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var brands = await ZoneApi.BodyAsync(response);
        var brand = Assert.Single(brands.EnumerateArray());
        Assert.Equal(DemoData.TriolBrandId, brand.GetProperty("id").GetString());
        Assert.Equal(7, brand.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Бренды_БезКатегории_Все()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/brands");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var brands = await ZoneApi.BodyAsync(response);
        Assert.Equal(6, brands.GetArrayLength());
    }
}
