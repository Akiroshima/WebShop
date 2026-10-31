using System.Net;
using System.Net.Http.Json;
using MurketApi.TestKit;

namespace MurketApi.Integration.B02;

/// <summary>B-02 · Вход: логин, email, телефон; ошибки входа.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    [Fact]
    public async Task ВходПоЛогину_200СтавитКукиAccess()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { login = "customer", password = "customer123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("murket_at", response.Headers.GetValues("Set-Cookie").First(),
            StringComparison.OrdinalIgnoreCase);
        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal("customer", body.GetProperty("user").GetProperty("login").GetString());
    }

    [Fact]
    public async Task ВходПоЭлектроннойПочтеИТелефону()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var byEmail = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { login = "ANNA.Smirnova@example.com", password = "customer123" });
        Assert.Equal(HttpStatusCode.OK, byEmail.StatusCode);

        var byPhone = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { login = "+7 999 123-45-67", password = "customer123" });
        Assert.Equal(HttpStatusCode.OK, byPhone.StatusCode);
    }

    [Fact]
    public async Task НеверныйПароль_401БезОракулаСуществования()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var wrongPassword = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { login = "customer", password = "wrong!" });
        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal("Неверный логин или пароль", ZoneApi.Message(await ZoneApi.BodyAsync(wrongPassword)));

        var unknownLogin = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { login = "ghost", password = "wrong!" });
        Assert.Equal(HttpStatusCode.Unauthorized, unknownLogin.StatusCode);
        Assert.Equal("Неверный логин или пароль", ZoneApi.Message(await ZoneApi.BodyAsync(unknownLogin)));
    }

    [Fact]
    public async Task ПустыеПоля_400ПоПолям()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { login = "", password = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.True(body.GetProperty("errors").TryGetProperty("login", out _));
        Assert.True(body.GetProperty("errors").TryGetProperty("password", out _));
    }
}
