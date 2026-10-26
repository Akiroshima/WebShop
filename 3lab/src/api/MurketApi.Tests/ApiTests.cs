using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

using MurketApi.Api;

namespace MurketApi.Tests;

/// <summary>
/// Интеграционные тесты HTTP-слоя: реальный конвейер (cookie, JWT, конверт ошибок, коды ответов).
/// </summary>
public sealed class ApiTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static WebApplicationFactory<Program> CreateFactory() =>
        new WebApplicationFactory<Program>().WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

    [Fact]
    public async Task Health_ОтвечаетОк()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ok", body.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Каталог_ДоступенБезАутентификации()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var categories = await client.GetAsync("/api/v1/catalog/categories");
        var products = await client.GetAsync("/api/v1/catalog/products?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, categories.StatusCode);
        Assert.Equal(HttpStatusCode.OK, products.StatusCode);
        var page = await products.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(2, page.GetProperty("items").GetArrayLength());
        Assert.Equal(39, page.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task КорзинаБезВхода_401ЕдиныйКонверт()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/cart");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("Требуется вход в аккаунт", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Вход_ВыставляетCookie_ИДаетДоступККорзине()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { login = "customer", password = "customer123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains("murket_at", login.Headers.GetValues("Set-Cookie").First(),
            StringComparison.OrdinalIgnoreCase);

        // CookieContainer клиента держит httpOnly murket_at/murket_rt.
        var me = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var cart = await client.GetAsync("/api/v1/cart");
        Assert.Equal(HttpStatusCode.OK, cart.StatusCode);
        var cartBody = await cart.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(3, cartBody.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task ВходНеверный_401_ИРолиРазграничены()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var bad = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { login = "customer", password = "wrong!" });
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
        var body = await bad.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("Неверный логин или пароль", body.GetProperty("message").GetString());

        // Покупатель не допускается в раздел «Управление».
        await client.PostAsJsonAsync("/api/v1/auth/login", new { login = "customer", password = "customer123" });
        var admin = await client.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, admin.StatusCode);
    }

    [Fact]
    public async Task ТоварНесуществующий_404_ВалютаОшибки()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products/p-nope");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("Товар не найден", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task НекорректныйQuery_400СОшибкойПоля()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/catalog/products?priceMin=abc");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("Данные заполнены неверно", body.GetProperty("message").GetString());
        Assert.True(body.GetProperty("errors").TryGetProperty("priceMin", out _));
    }

    [Fact]
    public async Task Logout_ОтзываетСессию()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/login",
            new { login = "customer", password = "customer123" });
        var logout = await client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        // Refresh-cookie отозван: обновление сессии отклоняется.
        var refresh = await client.PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Refresh_ПослеВходаОтдаётПользователя()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/login",
            new { login = "admin", password = "admin123" });
        var refresh = await client.PostAsync("/api/v1/auth/refresh", null);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var body = await refresh.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal("admin", body.GetProperty("user").GetProperty("login").GetString());
        Assert.Equal("admin", body.GetProperty("user").GetProperty("role").GetString());
    }
}

file static class WebApplicationFactoryExtensions
{
    /// <summary>Задаёт переменную окружения фабрике (используется до старта хоста).</summary>
    public static WebApplicationFactory<Program> WithEnvironment(
        this WebApplicationFactory<Program> factory, string key, string value) =>
        factory.WithWebHostBuilder(builder => builder.UseSetting(key, value));
}
