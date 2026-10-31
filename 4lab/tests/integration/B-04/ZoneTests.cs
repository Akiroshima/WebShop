using System.Net;
using System.Net.Http.Json;
using MurketApi.TestKit;

namespace MurketApi.Integration.B04;

/// <summary>B-04 · Восстановление доступа по email: запрос кода, подтверждение, сброс пароля.</summary>
public sealed class ZoneTests
{
    private const string Contact = "anna.smirnova@example.com";

    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    [Fact]
    public async Task ЗапросКода_200СКаналомИМаской()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/recovery/request", new { contact = Contact });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal("email", ZoneApi.String(body, "channel"));
        Assert.StartsWith("ann", ZoneApi.String(body, "maskedContact"));
    }

    [Fact]
    public async Task НесуществующийКонтакт_ТакойЖеОтвет()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/recovery/request",
            new { contact = "ghost@example.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal("email", ZoneApi.String(body, "channel"));
    }

    [Fact]
    public async Task НеверныйКод_400()
    {
        var api = await Api;
        using var client = api.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/recovery/request", new { contact = Contact });

        var response = await client.PostAsJsonAsync("/api/v1/auth/recovery/confirm",
            new { contact = Contact, code = "000000" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Код восстановления не подходит", ZoneApi.Message(await ZoneApi.BodyAsync(response)));
    }

    [Fact]
    public async Task ПолныйЦикл_КодСбросПароляНовыйВход()
    {
        var api = await Api;
        using var client = api.CreateClient();

        await client.PostAsJsonAsync("/api/v1/auth/recovery/request", new { contact = Contact });
        var code = await api.ReadRecoveryCodeAsync(Contact);

        var confirm = await client.PostAsJsonAsync("/api/v1/auth/recovery/confirm",
            new { contact = Contact, code });
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        var resetToken = ZoneApi.String(await ZoneApi.BodyAsync(confirm), "resetToken");
        Assert.NotEmpty(resetToken);

        var reset = await client.PostAsJsonAsync("/api/v1/auth/reset-password",
            new { resetToken, password = "НовыйПароль7!" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        // Старый пароль больше не подходит, новый — работает.
        var oldLogin = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { login = "customer", password = "customer123" });
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode);

        var newLogin = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { login = "customer", password = "НовыйПароль7!" });
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }
}
