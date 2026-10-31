using System.Net;
using System.Net.Http.Json;
using MurketApi.TestKit;

namespace MurketApi.Integration.B16;

/// <summary>B-16 · Профиль: чтение, обновление, конфликты уникальности.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    [Fact]
    public async Task Профиль_Чтение()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var response = await client.GetAsync("/api/v1/me/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal("Анна Смирнова", ZoneApi.String(body, "fullName"));
        Assert.Equal("anna.smirnova@example.com", ZoneApi.String(body, "email"));
        Assert.Equal("+79991234567", ZoneApi.String(body, "phone"));
    }

    [Fact]
    public async Task Профиль_ОбновлениеФИО()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        try
        {
            var response = await client.PutAsJsonAsync("/api/v1/me/profile",
                new { fullName = "Анна Кошкина", email = "anna.smirnova@example.com", phone = "+79991234567" });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await ZoneApi.BodyAsync(response);
            Assert.Equal("Анна Кошкина", ZoneApi.String(body, "fullName"));
        }
        finally
        {
            // Возврат демо-имени: другие тесты зоны читают профиль.
            var restore = await client.PutAsJsonAsync("/api/v1/me/profile",
                new { fullName = "Анна Смирнова", email = "anna.smirnova@example.com", phone = "+79991234567" });
            ZoneApi.EnsureSuccess(restore, "возврат демо-профиля");
        }
    }

    [Fact]
    public async Task Профиль_EmailЗанятАдмином_409()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var response = await client.PutAsJsonAsync("/api/v1/me/profile",
            new { fullName = "Анна", email = "admin@murket.ru", phone = "+79991234567" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Пользователь с таким email уже существует",
            ZoneApi.Message(await ZoneApi.BodyAsync(response)));
    }

    [Fact]
    public async Task Профиль_НекорректныйТелефон_400()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var response = await client.PutAsJsonAsync("/api/v1/me/profile",
            new { fullName = "Анна", email = "anna.smirnova@example.com", phone = "+7123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.True(body.GetProperty("errors").TryGetProperty("phone", out _));
    }
}
