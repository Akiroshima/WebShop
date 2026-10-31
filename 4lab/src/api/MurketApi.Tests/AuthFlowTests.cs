using MurketApi.Domain.Common;

namespace MurketApi.Tests;

/// <summary>Поток аутентификации и восстановления доступа на сервисах (реальная PostgreSQL).</summary>
public sealed class AuthFlowTests
{
    [Fact]
    public async Task Login_ПоискПоЛогинуEmailуТелефону()
    {
        await using var host = await TestHost.CreateAsync();

        Assert.Equal("customer", (await host.Auth.LoginAsync("customer", "customer123", "ip")).User.Login);
        Assert.Equal("customer",
            (await host.Auth.LoginAsync("ANNA.Smirnova@example.com", "customer123", "ip")).User.Login);
        Assert.Equal("customer",
            (await host.Auth.LoginAsync("+7 999 123-45-67", "customer123", "ip")).User.Login);
    }

    [Fact]
    public async Task Login_НеверныйПароль_ЕдиноеСообщениеБезОракула()
    {
        await using var host = await TestHost.CreateAsync();

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(
            () => host.Auth.LoginAsync("customer", "wrong!", "ip"));
        Assert.Equal("Неверный логин или пароль", ex.Message);

        await Assert.ThrowsAsync<UnauthorizedException>(() => host.Auth.LoginAsync("ghost", "wrong!", "ip"));
    }

    [Fact]
    public async Task Login_ПустыеПоля_ОшибкаВалидации()
    {
        await using var host = await TestHost.CreateAsync();

        var ex = await Assert.ThrowsAsync<ValidationException>(() => host.Auth.LoginAsync("", "", "ip"));
        Assert.Contains("login", ex.Errors.Keys);
        Assert.Contains("password", ex.Errors.Keys);
    }

    [Fact]
    public async Task Login_ПятьНеудачПодряд_Блокировка429()
    {
        await using var host = await TestHost.CreateAsync();

        for (var i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<UnauthorizedException>(
                () => host.Auth.LoginAsync("customer", "wrong!", "ip"));
        }

        var limited = await Assert.ThrowsAsync<RateLimitException>(
            () => host.Auth.LoginAsync("customer", "wrong!", "ip"));
        Assert.Equal("Слишком много попыток. Повторите позже", limited.Message);
    }

    [Fact]
    public async Task Login_УспехСбрасываетСчётчикНеудач()
    {
        await using var host = await TestHost.CreateAsync();

        for (var i = 0; i < 4; i++)
        {
            await Assert.ThrowsAsync<UnauthorizedException>(
                () => host.Auth.LoginAsync("customer", "wrong!", "ip"));
        }

        // Успешный вход не регистрируется как неудача.
        Assert.NotNull(await host.Auth.LoginAsync("customer", "customer123", "ip"));
        await Assert.ThrowsAsync<UnauthorizedException>(() => host.Auth.LoginAsync("customer", "wrong!", "ip"));
        Assert.NotNull(await host.Auth.LoginAsync("customer", "customer123", "ip"));
    }

    [Fact]
    public async Task Refresh_Ротация_СтарыйТокенУмирает()
    {
        await using var host = await TestHost.CreateAsync();
        var session = await host.Auth.LoginAsync("customer", "customer123", "ip");

        var rotated = await host.Auth.RefreshAsync(session.RefreshToken);
        Assert.Equal("customer", rotated.User.Login);

        // Повторное использование отозванного refresh — 401.
        await Assert.ThrowsAsync<UnauthorizedException>(() => host.Auth.RefreshAsync(session.RefreshToken));
    }

    [Fact]
    public async Task Восстановление_Email_ЗапросПодтверждениеСброс()
    {
        await using var host = await TestHost.CreateAsync();

        var sent = await host.Auth.RequestRecoveryCodeAsync("anna.smirnova@example.com");
        Assert.Equal("email", sent.Channel);
        var code = Assert.Single(host.Emails.Sent).Code;

        var confirm = await host.Auth.ConfirmRecoveryCodeAsync("anna.smirnova@example.com", code);
        Assert.NotEmpty(confirm.ResetToken);

        await host.Auth.ResetPasswordAsync(confirm.ResetToken, "НовыйПароль7!");
        Assert.NotNull(await host.Auth.LoginAsync("customer", "НовыйПароль7!", "ip"));
        await Assert.ThrowsAsync<UnauthorizedException>(
            () => host.Auth.LoginAsync("customer", "customer123", "ip"));
    }

    [Fact]
    public async Task Восстановление_Телефон_КодУходитПоСмс()
    {
        await using var host = await TestHost.CreateAsync();

        var sent = await host.Auth.RequestRecoveryCodeAsync("+79991234567");
        Assert.Equal("phone", sent.Channel);
        Assert.Single(host.Sms.Sent);
        Assert.Empty(host.Emails.Sent);
    }

    [Fact]
    public async Task Восстановление_НесуществующийКонтакт_ОдинаковыйОтвет200()
    {
        await using var host = await TestHost.CreateAsync();

        var sent = await host.Auth.RequestRecoveryCodeAsync("ghost@example.com");
        Assert.Equal("email", sent.Channel);
        Assert.Empty(host.Emails.Sent); // ничего не отправляем, но и не раскрываем отсутствие
    }

    [Fact]
    public async Task Восстановление_НеверныйКод_ПятьПопытокИСгорание()
    {
        await using var host = await TestHost.CreateAsync();
        await host.Auth.RequestRecoveryCodeAsync("anna.smirnova@example.com");

        for (var i = 0; i < 5; i++)
        {
            var ex = await Assert.ThrowsAsync<ValidationException>(
                () => host.Auth.ConfirmRecoveryCodeAsync("anna.smirnova@example.com", "000000"));
            Assert.Equal("Код восстановления не подходит", ex.Message);
        }

        // Код сгорел: даже верный больше не подходит.
        var code = Assert.Single(host.Emails.Sent).Code;
        await Assert.ThrowsAsync<ValidationException>(
            () => host.Auth.ConfirmRecoveryCodeAsync("anna.smirnova@example.com", code));
    }

    [Fact]
    public async Task Восстановление_КодЖивёт10Минут()
    {
        await using var host = await TestHost.CreateAsync();
        await host.Auth.RequestRecoveryCodeAsync("anna.smirnova@example.com");
        host.Clock.Advance(TimeSpan.FromMinutes(11));

        var code = Assert.Single(host.Emails.Sent).Code;
        await Assert.ThrowsAsync<ValidationException>(
            () => host.Auth.ConfirmRecoveryCodeAsync("anna.smirnova@example.com", code));
    }

    [Fact]
    public async Task Восстановление_ЛимитЗапросовКода_3Вчас()
    {
        await using var host = await TestHost.CreateAsync();
        const string contact = "anna.smirnova@example.com";

        await host.Auth.RequestRecoveryCodeAsync(contact);
        await host.Auth.RequestRecoveryCodeAsync(contact);
        await host.Auth.RequestRecoveryCodeAsync(contact);

        await Assert.ThrowsAsync<RateLimitException>(() => host.Auth.RequestRecoveryCodeAsync(contact));
    }

    [Fact]
    public async Task Сброс_СлабыйПарольОтвергнут()
    {
        await using var host = await TestHost.CreateAsync();
        await host.Auth.RequestRecoveryCodeAsync("anna.smirnova@example.com");
        var code = Assert.Single(host.Emails.Sent).Code;
        var token = (await host.Auth.ConfirmRecoveryCodeAsync("anna.smirnova@example.com", code)).ResetToken;

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => host.Auth.ResetPasswordAsync(token, "слабый"));
        Assert.Contains("password", ex.Errors.Keys);
    }

    [Fact]
    public async Task Сброс_ОтзываетВсеRefreshТокены()
    {
        await using var host = await TestHost.CreateAsync();
        var session = await host.Auth.LoginAsync("customer", "customer123", "ip");

        await host.Auth.RequestRecoveryCodeAsync("anna.smirnova@example.com");
        var code = Assert.Single(host.Emails.Sent).Code;
        var token = (await host.Auth.ConfirmRecoveryCodeAsync("anna.smirnova@example.com", code)).ResetToken;
        await host.Auth.ResetPasswordAsync(token, "НовыйПароль7!");

        // Refresh старой сессии отозван сменой пароля.
        await Assert.ThrowsAsync<UnauthorizedException>(() => host.Auth.RefreshAsync(session.RefreshToken));
    }
}
