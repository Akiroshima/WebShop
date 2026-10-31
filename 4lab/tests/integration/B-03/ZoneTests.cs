using System.Net;
using System.Net.Http.Json;
using MurketApi.TestKit;

namespace MurketApi.Integration.B03;

/// <summary>B-03 · Сессия: /auth/me, обновление по refresh-cookie, выход.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    [Fact]
    public async Task Me_БезВхода_401ЕдиныйКонверт()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Требуется вход в аккаунт", ZoneApi.Message(await ZoneApi.BodyAsync(response)));
    }

    [Fact]
    public async Task Me_ПослеВхода_ОтдаётПользователя()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var me = await client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var body = await ZoneApi.BodyAsync(me);
        Assert.Equal("customer", ZoneApi.String(body, "login"));
        Assert.Equal("customer", ZoneApi.String(body, "role"));
    }

    [Fact]
    public async Task Refresh_Ротация_ОтдаётПользователя()
    {
        var api = await Api;
        using var client = await api.LoginAsync("admin", "admin123");

        var refresh = await client.PostAsync("/api/v1/auth/refresh", null);

        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var body = await ZoneApi.BodyAsync(refresh);
        Assert.Equal("admin", body.GetProperty("user").GetProperty("login").GetString());
    }

    [Fact]
    public async Task Logout_204_ОтзываетRefreshСессию()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var logout = await client.PostAsync("/api/v1/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        // Refresh-cookie отозван: обновление сессии отклоняется.
        var refresh = await client.PostAsync("/api/v1/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }
}
