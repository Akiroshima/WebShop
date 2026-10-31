using System.Net;
using System.Net.Http.Json;
using MurketApi.TestKit;

namespace MurketApi.Integration.B05;

/// <summary>B-05 · Восстановление по телефону и лимиты запросов кода.</summary>
public sealed class ZoneTests
{
    private static readonly Task<ZoneApi> Api = ZoneApi.CreateAsync();

    [Fact]
    public async Task КодНаТелефон_КаналPhoneИМаска()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/recovery/request",
            new { contact = "+7 999 123-45-67" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.Equal("phone", ZoneApi.String(body, "channel"));
        Assert.StartsWith("+79991", ZoneApi.String(body, "maskedContact"));
    }

    [Fact]
    public async Task НекорректныйКонтакт_400()
    {
        var api = await Api;
        using var client = api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/recovery/request",
            new { contact = "не контакт" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await ZoneApi.BodyAsync(response);
        Assert.True(body.GetProperty("errors").TryGetProperty("contact", out _));
    }

    [Fact]
    public async Task ЛимитЗапросовКода_429ПослеТрёх()
    {
        var api = await Api;
        using var client = api.CreateClient();
        const string contact = "limiter@example.com";

        for (var i = 0; i < 3; i++)
        {
            var ok = await client.PostAsJsonAsync("/api/v1/auth/recovery/request", new { contact });
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        var limited = await client.PostAsJsonAsync("/api/v1/auth/recovery/request", new { contact });
        Assert.Equal((HttpStatusCode)429, limited.StatusCode);
    }
}
