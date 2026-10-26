using MurketApi.Application.Abstractions;
using MurketApi.Application.Dtos;
using MurketApi.Application.Mapping;
using MurketApi.Application.Validation;
using MurketApi.Domain.Common;
using MurketApi.Domain.Security;
using MurketApi.Domain.Users;

namespace MurketApi.Application.Services;

/// <summary>Результат успешной аутентификации: пользователь и пара новых токенов (сырые значения — в cookie).</summary>
public sealed record AuthResult(
    PublicUserDto User,
    string AccessToken,
    DateTimeOffset AccessExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAt);

/// <summary>
/// Аутентификация и восстановление доступа. Вход — по логину, email или телефону; ошибки входа не
/// раскрывают существование аккаунта; неудачные попытки сдерживаются скользящим окном на «логин|IP».
/// </summary>
public sealed class AuthService(
    IUserRepository users,
    ISecurityTokenRepository tokens,
    IPasswordHasher hasher,
    IAccessTokenService accessTokens,
    ISecretGenerator secrets,
    ITokenHasher tokenHasher,
    IEmailSender emailSender,
    ISmsSender smsSender,
    IRateLimiter rateLimiter,
    IKdfCounter kdf,
    IClock clock)
{
    public const int LoginRateLimit = 5;

    public static readonly TimeSpan LoginRateWindow = TimeSpan.FromSeconds(60);

    public const int RecoveryRateLimit = 3;

    public static readonly TimeSpan RecoveryRateWindow = TimeSpan.FromHours(1);

    public const int MaxCodeAttempts = 5;

    public static readonly TimeSpan RecoveryCodeTtl = TimeSpan.FromMinutes(10);

    public static readonly TimeSpan ResetTokenTtl = TimeSpan.FromMinutes(15);

    /// <summary>Вход: ровно одна KDF на попытку; сдерживание проверяется только на ветке неудачного пароля.</summary>
    public AuthResult Login(string? login, string? password, string clientIp)
    {
        var errors = new ValidationRules.Errors();
        var normalizedLogin = (login ?? string.Empty).Trim();
        if (normalizedLogin.Length == 0)
        {
            errors.Add("login", "Укажите логин");
        }

        if (string.IsNullOrEmpty(password))
        {
            errors.Add("password", "Укажите пароль");
        }

        if (errors.Any)
        {
            throw errors.ToException();
        }

        var rateKey = $"login|{normalizedLogin.ToLowerInvariant()}|{clientIp}";
        var user = users.FindForLogin(normalizedLogin);

        kdf.Increment("login");
        if (user is null || !hasher.Verify(user.PasswordHash, password!, "login"))
        {
            if (rateLimiter.CountRecent(rateKey, LoginRateWindow) >= LoginRateLimit)
            {
                throw new RateLimitException();
            }

            rateLimiter.Register(rateKey);
            throw new UnauthorizedException("Неверный логин или пароль");
        }

        return IssueSession(user);
    }

    public PublicUserDto Me(string userId)
    {
        var user = users.FindById(userId)
            ?? throw new UnauthorizedException("Сессия недействительна. Войдите снова");
        return user.ToPublicUser();
    }

    /// <summary>Обновление access-токена по refresh-cookie: старый refresh отзывается (ротация).</summary>
    public AuthResult Refresh(string? rawRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            throw new UnauthorizedException("Требуется вход в аккаунт");
        }

        var hash = tokenHasher.Hash(rawRefreshToken);
        var stored = tokens.FindRefreshToken(hash);
        if (stored is null || !stored.IsActive(clock.UtcNow))
        {
            throw new UnauthorizedException("Сессия недействительна. Войдите снова");
        }

        var user = users.FindById(stored.UserId)
            ?? throw new UnauthorizedException("Сессия недействительна. Войдите снова");

        stored.Revoke(clock.UtcNow);
        return IssueSession(user);
    }

    /// <summary>Выход: отзыв refresh-токена (cookie очищает контроллер).</summary>
    public void Logout(string? rawRefreshToken)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return;
        }

        var hash = tokenHasher.Hash(rawRefreshToken);
        var stored = tokens.FindRefreshToken(hash);
        stored?.Revoke(clock.UtcNow);
    }

    /// <summary>Запрос кода восстановления: 200 независимо от существования контакта (оракула нет).</summary>
    public RecoverySentDto RequestRecoveryCode(string? contact)
    {
        var rawContact = (contact ?? string.Empty).Trim();
        var errors = new ValidationRules.Errors();
        if (rawContact.Length == 0)
        {
            errors.Add("contact", "Укажите email или телефон");
            throw errors.ToException();
        }

        var channel = ValidationRules.DetectChannel(rawContact);
        var normalized = ValidationRules.NormalizeContact(rawContact, channel);
        if (!ValidationRules.IsValidContact(rawContact, channel))
        {
            errors.Add("contact", channel == RecoveryChannel.Email
                ? "Введите корректный email"
                : "Введите корректный номер телефона");
            throw errors.ToException();
        }

        var masked = ValidationRules.MaskContact(normalized, channel);
        var user = channel == RecoveryChannel.Email
            ? users.All().FirstOrDefault(candidate => candidate.Email == normalized)
            : users.All().FirstOrDefault(candidate => candidate.Phone == normalized);

        // Не раскрываем существование контакта: одинаковый ответ, одинаковый лимит на контакт.
        var rateKey = $"recovery|{normalized}";
        if (rateLimiter.CountRecent(rateKey, RecoveryRateWindow) >= RecoveryRateLimit)
        {
            throw new RateLimitException();
        }

        rateLimiter.Register(rateKey);
        if (user is null)
        {
            return new RecoverySentDto(ChannelName(channel), masked);
        }

        var code = secrets.NewRecoveryCode();
        tokens.ReplaceRecoveryCode(normalized, RecoveryCode.Create(
            Guid.NewGuid().ToString("N"), user.Id, channel, normalized,
            tokenHasher.Hash(code), clock.UtcNow + RecoveryCodeTtl, clock.UtcNow));

        if (channel == RecoveryChannel.Email)
        {
            emailSender.SendRecoveryCode(user.Email, code);
        }
        else
        {
            smsSender.SendRecoveryCode(user.Phone!, code);
        }

        return new RecoverySentDto(ChannelName(channel), masked);
    }

    /// <summary>Проверка кода → краткоживущий reset-токен; не более 5 неверных попыток на код.</summary>
    public ResetTokenDto ConfirmRecoveryCode(string? contact, string? code)
    {
        var rawContact = (contact ?? string.Empty).Trim();
        var errors = new ValidationRules.Errors();
        if (rawContact.Length == 0)
        {
            errors.Add("contact", "Укажите email или телефон");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            errors.Add("code", "Укажите код");
        }

        if (errors.Any)
        {
            throw errors.ToException();
        }

        var channel = ValidationRules.DetectChannel(rawContact);
        var normalized = ValidationRules.NormalizeContact(rawContact, channel);
        var stored = tokens.FindLiveRecoveryCode(normalized);
        if (stored is null || !stored.IsLive(clock.UtcNow))
        {
            throw new ValidationException("Код восстановления не подходит");
        }

        if (tokenHasher.Hash(code!.Trim()) != stored.CodeHash)
        {
            stored.RegisterFailedAttempt(MaxCodeAttempts);
            throw new ValidationException("Код восстановления не подходит");
        }

        stored.MarkUsed();
        tokens.SaveRecoveryCode(stored);

        var raw = secrets.NewResetToken();
        tokens.RevokeResetTokens(stored.UserId);
        tokens.AddResetToken(ResetToken.Create(
            tokenHasher.Hash(raw), stored.UserId, clock.UtcNow + ResetTokenTtl, clock.UtcNow));
        return new ResetTokenDto(raw);
    }

    /// <summary>Смена пароля по reset-токену; все refresh-токены пользователя отзываются.</summary>
    public void ResetPassword(string? rawResetToken, string? password)
    {
        var errors = new ValidationRules.Errors();
        if (string.IsNullOrWhiteSpace(rawResetToken))
        {
            errors.Add("resetToken", "Ссылка восстановления недействительна или истекла");
            throw errors.ToException();
        }

        ValidationRules.ValidatePassword(errors, password);
        if (errors.Any)
        {
            throw errors.ToException();
        }

        var stored = tokens.FindResetToken(tokenHasher.Hash(rawResetToken));
        if (stored is null || !stored.IsLive(clock.UtcNow))
        {
            throw new ValidationException("Ссылка восстановления недействительна или истекла");
        }

        var user = users.FindById(stored.UserId)
            ?? throw new ValidationException("Ссылка восстановления недействительна или истекла");

        kdf.Increment("reset_password");
        user.ChangePassword(hasher.Hash(password!, "reset_password"));
        stored.MarkUsed();
        tokens.SaveResetToken(stored);
        tokens.RevokeAllRefreshTokens(user.Id, clock.UtcNow);
    }

    private AuthResult IssueSession(User user)
    {
        var (accessToken, accessExpiresAt) = accessTokens.Issue(user.Id, user.Role.ToApiString());
        var rawRefresh = secrets.NewRefreshToken();
        var refreshExpiresAt = clock.UtcNow + TimeSpan.FromDays(7);
        tokens.AddRefreshToken(RefreshToken.Create(
            tokenHasher.Hash(rawRefresh), user.Id, refreshExpiresAt, clock.UtcNow));
        return new AuthResult(user.ToPublicUser(), accessToken, accessExpiresAt, rawRefresh, refreshExpiresAt);
    }

    private static string ChannelName(RecoveryChannel channel) =>
        channel == RecoveryChannel.Email ? "email" : "phone";
}
