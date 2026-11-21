using Microsoft.Extensions.Logging;

using MurketApi.Application.Abstractions;

namespace MurketApi.Infrastructure.Communication;

/// <summary>
/// Dev-отправка письма: полный текст с кодом — только в журнал (категория EmailDev, маркер [DEV-EMAIL]).
/// Это единственное место, где код восстановления появляется.
/// </summary>
public sealed class DevEmailSender(ILoggerFactory loggerFactory) : IEmailSender
{
    private readonly ILogger logger = loggerFactory.CreateLogger("EmailDev");

    public void SendRecoveryCode(string to, string code)
    {
        logger.LogInformation(
            "[DEV-EMAIL] Кому: {To} · Тема: {Subject} · Код восстановления доступа: {Code}",
            to, "Муркет: код восстановления доступа", code);
    }
}

/// <summary>Dev-«SMS»: номер и код — только в журнал (категория SmsDev, маркер [DEV-SMS]).</summary>
public sealed class DevSmsSender(ILoggerFactory loggerFactory) : ISmsSender
{
    private readonly ILogger logger = loggerFactory.CreateLogger("SmsDev");

    public void SendRecoveryCode(string to, string code)
    {
        logger.LogInformation("[DEV-SMS] Номер: {To} · Код восстановления доступа: {Code}", to, code);
    }
}

/// <summary>Остальные среды: письмо не отправляется и не логируется (один warning без адресата и кода).</summary>
public sealed class SilentEmailSender(ILogger<SilentEmailSender> logger) : IEmailSender
{
    public void SendRecoveryCode(string to, string code) =>
        logger.LogWarning("Отправка писем не настроена в этом окружении (SMTP отсутствует)");
}

/// <summary>Остальные среды: «SMS» не отправляется и не логируется (один warning без номера и кода).</summary>
public sealed class SilentSmsSender(ILogger<SilentSmsSender> logger) : ISmsSender
{
    public void SendRecoveryCode(string to, string code) =>
        logger.LogWarning("Отправка SMS не настроена в этом окружении (шлюз отсутствует)");
}
