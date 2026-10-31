namespace MurketApi.Domain.Security;

/// <summary>Refresh-токен: в системе хранится только хэш; отзывается при выходе и смене пароля.</summary>
public sealed class RefreshToken
{
    /// <summary>Суррогатный ключ строки (uuid в PostgreSQL); доменом не используется.</summary>
    public Guid Id { get; internal set; }

    public string Hash { get; internal set; } = string.Empty;

    public string UserId { get; internal set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; internal set; }

    public DateTimeOffset? RevokedAt { get; internal set; }

    public DateTimeOffset CreatedAt { get; internal set; }

    private RefreshToken()
    {
    }

    public static RefreshToken Create(string hash, string userId, DateTimeOffset expiresAt, DateTimeOffset now) =>
        new() { Hash = hash, UserId = userId, ExpiresAt = expiresAt, CreatedAt = now };

    public bool IsActive(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    public void Revoke(DateTimeOffset now) => RevokedAt ??= now;
}

/// <summary>Код восстановления доступа (6 цифр): хранится хэшем, TTL 10 минут, одноразовый.</summary>
public sealed class RecoveryCode
{
    public string Id { get; internal set; } = string.Empty;

    public string UserId { get; internal set; } = string.Empty;

    /// <summary>Канал доставки: email или телефон.</summary>
    public RecoveryChannel Channel { get; internal set; }

    /// <summary>Нормализованный контакт (email в нижнем регистре или телефон E.164).</summary>
    public string Contact { get; internal set; } = string.Empty;

    public string CodeHash { get; internal set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; internal set; }

    /// <summary>Число неверных попыток ввода; 5 — код сгорает.</summary>
    public int Attempts { get; internal set; }

    public bool Used { get; internal set; }

    public DateTimeOffset CreatedAt { get; internal set; }

    private RecoveryCode()
    {
    }

    public static RecoveryCode Create(string id, string userId, RecoveryChannel channel, string contact,
        string codeHash, DateTimeOffset expiresAt, DateTimeOffset now) => new()
    {
        Id = id,
        UserId = userId,
        Channel = channel,
        Contact = contact,
        CodeHash = codeHash,
        ExpiresAt = expiresAt,
        CreatedAt = now,
    };

    public bool IsLive(DateTimeOffset now) => !Used && ExpiresAt > now;

    public void MarkUsed() => Used = true;

    public void RegisterFailedAttempt(int maxAttempts = 5)
    {
        Attempts++;
        if (Attempts >= maxAttempts)
        {
            Used = true;
        }
    }
}

public enum RecoveryChannel
{
    Email = 0,
    Phone,
}

/// <summary>Краткоживущий токен сброса пароля (TTL 15 минут): хранится хэшем, одноразовый.</summary>
public sealed class ResetToken
{
    public string Hash { get; internal set; } = string.Empty;

    public string UserId { get; internal set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; internal set; }

    public bool Used { get; internal set; }

    public DateTimeOffset CreatedAt { get; internal set; }

    private ResetToken()
    {
    }

    public static ResetToken Create(string hash, string userId, DateTimeOffset expiresAt, DateTimeOffset now) =>
        new() { Hash = hash, UserId = userId, ExpiresAt = expiresAt, CreatedAt = now };

    public bool IsLive(DateTimeOffset now) => !Used && ExpiresAt > now;

    public void MarkUsed() => Used = true;
}
