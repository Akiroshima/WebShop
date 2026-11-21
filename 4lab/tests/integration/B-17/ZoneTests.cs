using System.Net;
using System.Net.Http.Json;
using MurketApi.TestKit;

namespace MurketApi.Integration.B17;

/// <summary>
/// B-17 · Смена пароля: успех, отзыв прочих сессий, неверный текущий.
/// Смена пароля необратима политикой (демо-пароль без спецзнака нельзя установить через API),
/// поэтому каждый тест работает на собственной изолированной базе.
/// </summary>
public sealed class ZoneTests
{
    [Fact]
    public async Task НеверныйТекущийПароль_400()
    {
        await using var api = await ZoneApi.CreateAsync();
        using var client = await api.LoginAsync("customer", "customer123");

        var response = await client.PutAsJsonAsync("/api/v1/me/password",
            new { currentPassword = "wrong!", password = "НовыйПароль7!" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Неверный текущий пароль", ZoneApi.Message(await ZoneApi.BodyAsync(response)));
    }

    [Fact]
    public async Task СменаПароля_НовыйРаботаетСтарыйОтказывает()
    {
        await using var api = await ZoneApi.CreateAsync();
        using var client = await api.LoginAsync("customer", "customer123");

        var change = await client.PutAsJsonAsync("/api/v1/me/password",
            new { currentPassword = "customer123", password = "НовыйПароль7!" });
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);

        // Сессия жива, вход по старому паролю отклоняется.
        var me = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        using var fresh = api.CreateClient();
        var oldLogin = await fresh.PostAsJsonAsync("/api/v1/auth/login",
            new { login = "customer", password = "customer123" });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await fresh.PostAsJsonAsync("/api/v1/auth/login",
            new { login = "customer", password = "НовыйПароль7!" });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task СменаПароля_ОтзываетПрочиеСессии()
    {
        await using var api = await ZoneApi.CreateAsync();
        using var other = await api.LoginAsync("customer", "customer123");
        var otherRefresh = await other.PostAsync("/api/v1/auth/refresh", null);
        ZoneApi.EnsureSuccess(otherRefresh, "refresh второй сессии");

        using var current = await api.LoginAsync("customer", "customer123");
        var change = await current.PutAsJsonAsync("/api/v1/me/password",
            new { currentPassword = "customer123", password = "ЕщёПароль7!" });
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);

        // Вторая сессия отозвана: её refresh больше не действует.
        var revived = await other.PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, revived.StatusCode);
    }
}
