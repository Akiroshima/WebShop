using System.Security.Cryptography;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MurketApi.Domain.Users;
using MurketApi.Infrastructure.Seeding;

namespace MurketApi.Api.Infrastructure;

/// <summary>Параметры аутентификации (Auth__*). Нарушения правил останавливают старт приложения.</summary>
public sealed class AuthOptions
{
    public const int MinJwtKeyLength = 32;

    private AuthOptions(string jwtKey, TimeSpan accessTtl, TimeSpan refreshTtl, int pbkdf2Iterations)
    {
        JwtKey = jwtKey;
        AccessTtl = accessTtl;
        RefreshTtl = refreshTtl;
        Pbkdf2Iterations = pbkdf2Iterations;
    }

    public string JwtKey { get; }

    public TimeSpan AccessTtl { get; }

    public TimeSpan RefreshTtl { get; }

    public int Pbkdf2Iterations { get; }

    public static AuthOptions FromEnvironment(IConfiguration configuration, bool isDevelopment,
        IHostEnvironment environment)
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<AuthOptions>();

        // Auth__JwtKey: в Production обязательна (≥ 32 символов); в Development без неё используется
        // эпизодический случайный ключ — токены не переживают перезапуск (пишется warning).
        var jwtKey = configuration["Auth__JwtKey"];
        if (string.IsNullOrWhiteSpace(jwtKey))
        {
            if (isDevelopment)
            {
                jwtKey = RandomNumberGenerator.GetString(
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 64);
                logger.LogWarning(
                    "Auth__JwtKey не задана: в Development используется эпизодический случайный ключ; " +
                    "токены не переживут перезапуск (окружение {Environment})",
                    environment.EnvironmentName);
            }
            else
            {
                throw new InvalidOperationException(
                    "Auth__JwtKey: обязательна в Production (не менее 32 символов)");
            }
        }
        else if (jwtKey.Length < MinJwtKeyLength)
        {
            throw new InvalidOperationException(
                $"Auth__JwtKey: должна содержать не менее {MinJwtKeyLength} символов");
        }

        var accessMinutes = ReadPositiveInt(configuration, "Auth__AccessTtlMinutes", 15, logger);
        var accessTtl = TimeSpan.FromMinutes(accessMinutes);
        var refreshTtl = TimeSpan.FromDays(ReadPositiveInt(configuration, "Auth__RefreshTtlDays", 7, logger));

        var iterations = configuration["Auth__Pbkdf2Iterations"];
        var pbkdf2 = 210000;
        if (!string.IsNullOrWhiteSpace(iterations))
        {
            if (!int.TryParse(iterations, out var parsed) || parsed is < 1 or > 10_000_000)
            {
                throw new InvalidOperationException(
                    "Auth__Pbkdf2Iterations: целое от 1 до 10 000 000");
            }

            pbkdf2 = parsed;
        }

        loggerFactory.Dispose();
        return new AuthOptions(jwtKey, accessTtl, refreshTtl, pbkdf2);
    }

    private static int ReadPositiveInt(IConfiguration configuration, string name, int fallback, ILogger logger)
    {
        var raw = configuration[name];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return fallback;
        }

        if (!int.TryParse(raw, out var value) || value <= 0)
        {
            throw new InvalidOperationException($"{name}: целое больше 0");
        }

        return value;
    }
}

/// <summary>Чтение Seed__*-переменных; в Production действует guard SEC-001.</summary>
public static class SeedOptionsReader
{
    public static SeedOptions FromEnvironment(IConfiguration configuration,
        bool isDevelopment, IHostEnvironment environment)
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger(typeof(SeedOptionsReader));

        var options = new SeedOptions
        {
            AdminLogin = Fallback(configuration["Seed__AdminLogin"], "admin"),
            AdminPassword = configuration["Seed__AdminPassword"],
            CustomerLogin = Fallback(configuration["Seed__CustomerLogin"], "customer"),
            CustomerPassword = configuration["Seed__CustomerPassword"],
        };

        if (string.IsNullOrWhiteSpace(options.AdminLogin) || string.IsNullOrWhiteSpace(options.CustomerLogin))
        {
            throw new InvalidOperationException("Seed__AdminLogin и Seed__CustomerLogin: не могут быть пустыми");
        }

        // SEC-001: в Production пароли сид-учёток обязательны и не могут быть значениями по умолчанию.
        if (!isDevelopment)
        {
            GuardSeedPassword("Seed__AdminPassword", options.AdminPassword, "admin123");
            GuardSeedPassword("Seed__CustomerPassword", options.CustomerPassword, "customer123");
        }
        else
        {
            options.AdminPassword ??= "admin123";
            options.CustomerPassword ??= "customer123";
        }

        // Seed__DemoData: допустимы true/false; некорректное значение — warning и умолчание окружения.
        var demoRaw = configuration["Seed__DemoData"];
        if (!string.IsNullOrWhiteSpace(demoRaw))
        {
            if (bool.TryParse(demoRaw, out var demo))
            {
                options.DemoData = demo;
            }
            else
            {
                logger.LogWarning(
                    "Seed__DemoData: некорректное значение «{Value}» (допустимы true/false); " +
                    "использовано умолчание окружения {Default}",
                    demoRaw, isDevelopment);
            }
        }

        loggerFactory.Dispose();
        return options;
    }

    private static void GuardSeedPassword(string name, string? password, string defaultValue)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException($"{name}: обязательна в Production (см. SEC-001 в README)");
        }

        if (password == defaultValue)
        {
            throw new InvalidOperationException(
                $"{name}: не может быть значением по умолчанию в Production (см. SEC-001 в README)");
        }

        if (!PasswordPolicy.IsAcceptable(password))
        {
            throw new InvalidOperationException(
                $"{name}: пароль не удовлетворяет правилам системы (8–128 символов, цифра, буква, спецзнак)");
        }
    }

    private static string Fallback(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;
}
