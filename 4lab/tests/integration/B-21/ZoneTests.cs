using System.Net;
using System.Net.Http.Json;
using MurketApi.TestKit;

namespace MurketApi.Integration.B21;

/// <summary>B-21 · Администрирование: пользователи системы и границы роли.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    [Fact]
    public async Task Пользователи_200ДваУчётныхЗаписи()
    {
        var api = await Api;
        using var client = await api.LoginAsync("admin", "admin123");

        var response = await client.GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await ZoneApi.BodyAsync(response);
        Assert.Equal(2, items.GetArrayLength());
        Assert.Contains(items.EnumerateArray(),
            user => user.GetProperty("login").GetString() == "customer"
                && user.GetProperty("role").GetString() == "customer");
        Assert.Contains(items.EnumerateArray(),
            user => user.GetProperty("login").GetString() == "admin"
                && user.GetProperty("role").GetString() == "admin");
    }

    [Fact]
    public async Task Покупателю_403()
    {
        var api = await Api;
        using var client = await api.LoginAsync("customer", "customer123");

        var response = await client.GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Доступ запрещён", ZoneApi.Message(await ZoneApi.BodyAsync(response)));
    }

    [Fact]
    public async Task БезВхода_401()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.GetAsync("/api/v1/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
