namespace MurketApi.Application.Abstractions;

/// <summary>
/// Хэширование паролей: PBKDF2-HMAC-SHA256 (соль и параметры — внутри строки хэша). Каждая операция
/// (Hash и Verify — это KDF) инкрементирует метрику auth_kdf_operations_total{caller}.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password, string caller = "reference");

    bool Verify(string hash, string password, string caller);
}

/// <summary>Выпуск access-JWT (HS256) для httpOnly-cookie.</summary>
public interface IAccessTokenService
{
    (string Token, DateTimeOffset ExpiresAt) Issue(string userId, string role);
}

/// <summary>Быстрое хэширование секретов (refresh/reset-токены, коды восстановления): солёный SHA-256.</summary>
public interface ITokenHasher
{
    string Hash(string secret);
}

/// <summary>Генерация случайных секретов.</summary>
public interface ISecretGenerator
{
    /// <summary>Refresh-токен: 256 бит энтропии в base64url.</summary>
    string NewRefreshToken();

    /// <summary>6-значный код восстановления.</summary>
    string NewRecoveryCode();

    /// <summary>Reset-токен восстановления пароля.</summary>
    string NewResetToken();
}

/// <summary>Отправка письма с кодом восстановления (dev-реализация пишет в журнал).</summary>
public interface IEmailSender
{
    void SendRecoveryCode(string to, string code);
}

/// <summary>Отправка «SMS» с кодом восстановления (dev-реализация пишет в журнал).</summary>
public interface ISmsSender
{
    void SendRecoveryCode(string to, string code);
}

/// <summary>Скользящее окно частоты попыток (без распределённого хранилища — достаточно для одного процесса).</summary>
public interface IRateLimiter
{
    /// <summary>Число зарегистрированных событий за последние window.</summary>
    int CountRecent(string key, TimeSpan window);

    /// <summary>Регистрирует событие сейчас.</summary>
    void Register(string key);
}

/// <summary>Счётчик операций вывода ключа (PBKDF2): метрика auth_kdf_operations_total{caller}.</summary>
public interface IKdfCounter
{
    void Increment(string caller);
}

/// <summary>
/// Единица работы: многорепозитарные операции выполняются атомарно. Реализация на PostgreSQL —
/// транзакция базы данных на соединении текущего запроса: EF Core и «сырые» SQL-команды
/// репозитория заказов commit-ятся вместе.
/// </summary>
public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation);

    Task ExecuteAsync(Func<Task> operation);
}
