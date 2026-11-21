using MurketApi.Domain.Common;

namespace MurketApi.Tests;

/// <summary>Поток аутентификации и восстановления доступа на сервисах (заглушки БД).</summary>
public sealed class AuthFlowTests
{
    [Fact]
    public void Login_ПоискПоЛогинуEmailуТелефону()
    {
        var host = new TestHost();

        Assert.Equal("customer", host.Auth.Login("customer", "customer123", "ip").User.Login);
        Assert.Equal("customer", host.Auth.Login("ANNA.Smirnova@example.com", "customer123", "ip").User.Login);
        Assert.Equal("customer", host.Auth.Login("+7 999 123-45-67", "customer123", "ip").User.Login);
    }

    [Fact]
    public void Login_НеверныйПароль_ЕдиноеСообщениеБезОракула()
    {
        var host = new TestHost();

        var ex = Assert.Throws<UnauthorizedException>(
            () => host.Auth.Login("customer", "wrong!", "ip"));
        Assert.Equal("Неверный логин или пароль", ex.Message);

        Assert.Throws<UnauthorizedException>(() => host.Auth.Login("ghost", "wrong!", "ip"));
    }

    [Fact]
    public void Login_ПустыеПоля_ОшибкаВалидации()
    {
        var host = new TestHost();

        var ex = Assert.Throws<ValidationException>(() => host.Auth.Login("", "", "ip"));
        Assert.Contains("login", ex.Errors.Keys);
        Assert.Contains("password", ex.Errors.Keys);
    }

    [Fact]
    public void Login_ПятьНеудачПодряд_Блокировка429()
    {
        var host = new TestHost();

        for (var i = 0; i < 5; i++)
        {
            Assert.Throws<UnauthorizedException>(() => host.Auth.Login("customer", "wrong!", "ip"));
        }

        var limited = Assert.Throws<RateLimitException>(() => host.Auth.Login("customer", "wrong!", "ip"));
        Assert.Equal("Слишком много попыток. Повторите позже", limited.Message);
    }

    [Fact]
    public void Login_УспехСбрасываетСчётчикНеудач()
    {
        var host = new TestHost();

        for (var i = 0; i < 4; i++)
        {
            Assert.Throws<UnauthorizedException>(() => host.Auth.Login("customer", "wrong!", "ip"));
        }

        // Успешный вход не регистрируется как неудача.
        Assert.NotNull(host.Auth.Login("customer", "customer123", "ip"));
        Assert.Throws<UnauthorizedException>(() => host.Auth.Login("customer", "wrong!", "ip"));
        Assert.NotNull(host.Auth.Login("customer", "customer123", "ip"));
    }

    [Fact]
    public void Refresh_Ротация_СтарыйТокенУмирает()
    {
        var host = new TestHost();
        var session = host.Auth.Login("customer", "customer123", "ip");

        var rotated = host.Auth.Refresh(session.RefreshToken);
        Assert.Equal("customer", rotated.User.Login);

        // Повторное использование отозванного refresh — 401.
        Assert.Throws<UnauthorizedException>(() => host.Auth.Refresh(session.RefreshToken));
    }

    [Fact]
    public void Восстановление_Email_ЗапросПодтверждениеСброс()
    {
        var host = new TestHost();

        var sent = host.Auth.RequestRecoveryCode("anna.smirnova@example.com");
        Assert.Equal("email", sent.Channel);
        var code = Assert.Single(host.Emails.Sent).Code;

        var confirm = host.Auth.ConfirmRecoveryCode("anna.smirnova@example.com", code);
        Assert.NotEmpty(confirm.ResetToken);

        host.Auth.ResetPassword(confirm.ResetToken, "НовыйПароль7!");
        Assert.NotNull(host.Auth.Login("customer", "НовыйПароль7!", "ip"));
        Assert.Throws<UnauthorizedException>(() => host.Auth.Login("customer", "customer123", "ip"));
    }

    [Fact]
    public void Восстановление_Телефон_КодУходитПоСмс()
    {
        var host = new TestHost();

        var sent = host.Auth.RequestRecoveryCode("+79991234567");
        Assert.Equal("phone", sent.Channel);
        Assert.Single(host.Sms.Sent);
        Assert.Empty(host.Emails.Sent);
    }

    [Fact]
    public void Восстановление_НесуществующийКонтакт_ОдинаковыйОтвет200()
    {
        var host = new TestHost();

        var sent = host.Auth.RequestRecoveryCode("ghost@example.com");
        Assert.Equal("email", sent.Channel);
        Assert.Empty(host.Emails.Sent); // ничего не отправляем, но и не раскрываем отсутствие
    }

    [Fact]
    public void Восстановление_НеверныйКод_ПятьПопытокИСгорание()
    {
        var host = new TestHost();
        host.Auth.RequestRecoveryCode("anna.smirnova@example.com");

        for (var i = 0; i < 5; i++)
        {
            var ex = Assert.Throws<ValidationException>(
                () => host.Auth.ConfirmRecoveryCode("anna.smirnova@example.com", "000000"));
            Assert.Equal("Код восстановления не подходит", ex.Message);
        }

        // Код сгорел: даже верный больше не подходит.
        var code = Assert.Single(host.Emails.Sent).Code;
        Assert.Throws<ValidationException>(
            () => host.Auth.ConfirmRecoveryCode("anna.smirnova@example.com", code));
    }

    [Fact]
    public void Восстановление_КодЖивёт10Минут()
    {
        var host = new TestHost();
        host.Auth.RequestRecoveryCode("anna.smirnova@example.com");
        host.Clock.Advance(TimeSpan.FromMinutes(11));

        var code = Assert.Single(host.Emails.Sent).Code;
        Assert.Throws<ValidationException>(
            () => host.Auth.ConfirmRecoveryCode("anna.smirnova@example.com", code));
    }

    [Fact]
    public void Восстановление_ЛимитЗапросовКода_3Вчас()
    {
        var host = new TestHost();
        const string contact = "anna.smirnova@example.com";

        host.Auth.RequestRecoveryCode(contact);
        host.Auth.RequestRecoveryCode(contact);
        host.Auth.RequestRecoveryCode(contact);

        Assert.Throws<RateLimitException>(() => host.Auth.RequestRecoveryCode(contact));
    }

    [Fact]
    public void Сброс_СлабыйПарольОтвергнут()
    {
        var host = new TestHost();
        host.Auth.RequestRecoveryCode("anna.smirnova@example.com");
        var code = Assert.Single(host.Emails.Sent).Code;
        var token = host.Auth.ConfirmRecoveryCode("anna.smirnova@example.com", code).ResetToken;

        var ex = Assert.Throws<ValidationException>(() => host.Auth.ResetPassword(token, "слабый"));
        Assert.Contains("password", ex.Errors.Keys);
    }

    [Fact]
    public void Сброс_ОтзываетВсеRefreshТокены()
    {
        var host = new TestHost();
        var session = host.Auth.Login("customer", "customer123", "ip");

        host.Auth.RequestRecoveryCode("anna.smirnova@example.com");
        var code = Assert.Single(host.Emails.Sent).Code;
        var token = host.Auth.ConfirmRecoveryCode("anna.smirnova@example.com", code).ResetToken;
        host.Auth.ResetPassword(token, "НовыйПароль7!");

        // Refresh старой сессии отозван сменой пароля.
        Assert.Throws<UnauthorizedException>(() => host.Auth.Refresh(session.RefreshToken));
    }
}
