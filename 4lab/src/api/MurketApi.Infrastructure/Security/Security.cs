using System.Security.Cryptography;

using MurketApi.Application.Abstractions;

namespace MurketApi.Infrastructure.Security;

/// <summary>
/// PBKDF2-HMAC-SHA256. Формат хэша: pbkdf2-sha256$iterations$saltB64$hashB64 — параметры Verify
/// читает из самого хэша, поэтому смена числа итераций не ломает существующие пароли.
/// </summary>
public sealed class Pbkdf2PasswordHasher(int iterations, IKdfCounter kdf) : IPasswordHasher
{
    private const int SaltSize = 16;

    private const int HashSize = 32;

    public string Hash(string password, string caller = "reference")
    {
        kdf.Increment(caller);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, HashSize);
        return $"pbkdf2-sha256${iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string hash, string password, string caller)
    {
        kdf.Increment(caller);
        var parts = hash.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2-sha256"
            || !int.TryParse(parts[1], out var storedIterations)
            || storedIterations is < 1 or > 10_000_000)
        {
            return false;
        }

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password ?? string.Empty, salt, storedIterations,
            HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}

/// <summary>
/// Быстрый детерминированный хэш секретов: SHA-256 с фиксированной «перцем» развёртывания.
/// Перцем выступает ключ JWT (или Auth__TokenHashSalt): хэши переживают перезапуск процесса,
/// поэтому refresh-токены и коды восстановления, сохранённые в PostgreSQL, остаются действительными.
/// </summary>
public sealed class SaltedSha256TokenHasher(string pepper) : ITokenHasher
{
    private readonly byte[] pepperBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(pepper));

    public string Hash(string secret)
    {
        var hash = SHA256.HashData([.. pepperBytes, .. System.Text.Encoding.UTF8.GetBytes(secret ?? string.Empty)]);
        return Convert.ToHexString(hash);
    }
}

/// <summary>Генерация секретов: refresh/reset — 256 бит энтропии, код восстановления — 6 цифр.</summary>
public sealed class CryptoSecretGenerator : ISecretGenerator
{
    public string NewRefreshToken() => RandomNumberGenerator.GetString("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 43);

    public string NewRecoveryCode() => Random.Shared.Next(100000, 1000000).ToString();

    public string NewResetToken() => RandomNumberGenerator.GetString("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 43);
}
