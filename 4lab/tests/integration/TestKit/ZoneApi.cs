using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

using MurketApi.Api;

using Npgsql;

namespace MurketApi.TestKit;

/// <summary>
/// HTTP-контекст интеграционной зоны: изолированная база PostgreSQL (клон шаблона с демо-данными)
/// и WebApplicationFactory поверх реального Program. Один экземпляр на зону: тесты зоны выполняются
/// последовательно и работают с одной и той же демо-базой.
/// </summary>
public sealed class ZoneApi : IAsyncDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Фиксированный «перец» хэша секретов в зоне: тесты восстанавливают 6-значный код по хэшу
    /// из базы (в HTTP-контракте код не раскрывается — в Development он пишется в журнал бэкенда).
    /// </summary>
    public const string Pepper = "zone-tests-pepper";

    private readonly TestDatabase database;

    private readonly WebApplicationFactory<Program> factory;

    private ZoneApi(TestDatabase database)
    {
        this.database = database;
        // Строка подключения доходит до Program двумя путями: настройкой фабрики и переменной
        // окружения процесса (каждая зона выполняется в своём testhost).
        Environment.SetEnvironmentVariable("Database__ConnectionString", database.ConnectionString);
        Environment.SetEnvironmentVariable("Auth__TokenHashSalt", Pepper);
        factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder
                .UseSetting("ASPNETCORE_ENVIRONMENT", "Development")
                .UseSetting("Database__ConnectionString", database.ConnectionString)
                .UseSetting("Auth__TokenHashSalt", Pepper)
                .UseSetting("Auth__Pbkdf2Iterations", "1000"));
    }

    /// <summary>Клиент с собственным CookieContainer (httpOnly-cookie сессий изолируются по клиенту).</summary>
    public HttpClient CreateClient() => factory.CreateClient();

    /// <summary>Клиент, прошедший вход под указанной демо-учёткой.</summary>
    public async Task<HttpClient> LoginAsync(string login, string password)
    {
        var client = CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { login, password });
        EnsureSuccess(response, "вход в демо-учётку");
        return client;
    }

    /// <summary>Прямой SQL к базе зоны (сквозные проверки B-23). Параметры — @p0, @p1, …</summary>
    public async Task<object?[]> QueryAsync(string sql, params object[] parameters)
    {
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        for (var i = 0; i < parameters.Length; i++)
        {
            command.Parameters.AddWithValue($"p{i}", parameters[i]);
        }

        await using var reader = await command.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var values = new object?[reader.FieldCount];
            reader.GetValues(values!);
            return values;
        }

        return [];
    }

    /// <summary>Подбирает 6-значный код восстановления по хэшу из базы (перец зоны известен).</summary>
    public async Task<string> ReadRecoveryCodeAsync(string contact)
    {
        var row = await QueryAsync(
            "SELECT code_hash FROM recovery_codes WHERE contact = @p0 ORDER BY created_at DESC LIMIT 1",
            contact);
        var hash = (string)row[0]!;
        for (var i = 0; i < 1_000_000; i++)
        {
            var candidate = i.ToString("D6");
            if (Hash(candidate) == hash)
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Код восстановления не подобран по хэшу");
    }

    private static string Hash(string secret)
    {
        var pepper = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(Pepper));
        var hash = System.Security.Cryptography.SHA256.HashData(
            [.. pepper, .. System.Text.Encoding.UTF8.GetBytes(secret)]);
        return Convert.ToHexString(hash);
    }

    public async ValueTask DisposeAsync()
    {
        factory.Dispose();
        await database.DisposeAsync();
    }

    public static async Task<ZoneApi> CreateAsync()
    {
        var database = await TestDatabases.CloneDemoAsync();
        return new ZoneApi(database);
    }

    // ----- Помощники утверждений -----

    public static async Task<JsonElement> BodyAsync(HttpResponseMessage response)
    {
        using var document = await response.Content.ReadFromJsonAsync<JsonDocument>();
        return document!.RootElement.Clone();
    }

    public static void EnsureSuccess(HttpResponseMessage response, string action)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Ожидался успех при «{action}», получен {(int)response.StatusCode} {response.StatusCode}");
        }
    }

    public static string Message(JsonElement body) => body.GetProperty("message").GetString()!;

    public static string String(JsonElement element, string property) => element.GetProperty(property).GetString()!;
}
