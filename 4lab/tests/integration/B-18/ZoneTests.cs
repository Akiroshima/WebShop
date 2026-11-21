using System.Net;
using System.Net.Http.Json;
using MurketApi.Infrastructure.Seeding;
using MurketApi.TestKit;

namespace MurketApi.Integration.B18;

/// <summary>B-18 · Адреса доставки: создание, единственный «по умолчанию», удаление.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    private static object Draft(string house, bool isDefault) => new
    {
        label = "Тест",
        recipient = "Анна Смирнова",
        phone = "+79991234567",
        city = "Москва",
        street = "ул. Тестовая",
        house,
        apartment = "1",
        entrance = "2",
        floor = "3",
        intercom = "45",
        postalCode = "101000",
        isDefault,
    };

    [Fact]
    public async Task Создать_201ИВозвратВДжсоне()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var response = await client.PostAsJsonAsync("/api/v1/me/addresses", Draft("101", false));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal("Тест", ZoneApi.String(body, "label"));
        Assert.False(body.GetProperty("isDefault").GetBoolean());
    }

    [Fact]
    public async Task НовыйАдресПоУмолчанию_СбрасываетПрежний()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var created = await client.PostAsJsonAsync("/api/v1/me/addresses", Draft("102", true));
        ZoneApi.EnsureSuccess(created, "создание адреса по умолчанию");

        var list = await ZoneApi.BodyAsync(await client.GetAsync("/api/v1/me/addresses"));
        var defaults = list.EnumerateArray()
            .Where(address => address.GetProperty("isDefault").GetBoolean()).ToList();
        var d = Assert.Single(defaults);
        Assert.Equal("102", ZoneApi.String(d, "house"));
    }

    [Fact]
    public async Task ЧужойАдрес_404()
    {
        var api = await Api;
        using var admin = await api.LoginAsync("admin", "admin123");

        var response = await admin.PutAsJsonAsync($"/api/v1/me/addresses/{DemoData.HomeAddressId}",
            Draft("103", false));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Адрес не найден", ZoneApi.Message(await ZoneApi.BodyAsync(response)));
    }

    [Fact]
    public async Task Удалить_204()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");
        var created = await client.PostAsJsonAsync("/api/v1/me/addresses", Draft("104", false));
        var id = ZoneApi.String(await ZoneApi.BodyAsync(created), "id");

        var response = await client.DeleteAsync($"/api/v1/me/addresses/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var list = await ZoneApi.BodyAsync(await client.GetAsync("/api/v1/me/addresses"));
        Assert.DoesNotContain(list.EnumerateArray(),
            address => address.GetProperty("id").GetString() == id);
    }
}
