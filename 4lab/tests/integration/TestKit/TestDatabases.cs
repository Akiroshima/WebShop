using System.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using MurketApi.Infrastructure.Metrics;
using MurketApi.Infrastructure.Persistence;
using MurketApi.Infrastructure.Security;
using MurketApi.Infrastructure.Seeding;

using Npgsql;

namespace MurketApi.TestKit;

/// <summary>
/// Изолированные тестовые базы PostgreSQL. Один раз на процесс создаются два шаблона:
/// пустая схема (02_schema.sql) и схема с демо-данными (сид приложения). Каждый тест/зона
/// получают собственную базу мгновенным клоном шаблона (TEMPLATE) и удаляют её после себя.
/// Подключение: переменная MurketTests__ConnectionString (по умолчанию — локальная база murket,
/// роль murket с правом CREATEDB, см. src/api/db/01_database.sql).
/// </summary>
public static class TestDatabases
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private static readonly string Prefix = $"murket_t_{Environment.ProcessId:x}";

    private static bool templatesReady;

    static TestDatabases()
    {
        // После жёсткого завершения testhost (таймаут, отмена) тестовые базы остаются; Windows
        // переиспользует pid, поэтому новый процесс с тем же префиксом мог бы столкнуться с ними.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => DropAllForPrefix();
    }

    public static string ServerConnectionString { get; } =
        Environment.GetEnvironmentVariable("MurketTests__ConnectionString")
        ?? "Host=127.0.0.1;Port=5432;Database=postgres;Username=murket;Password=murket";

    /// <summary>База-шаблон с демо-данными сида (пользователи, каталог, корзина, 3 заказа).</summary>
    public static async Task<TestDatabase> CloneDemoAsync()
    {
        await EnsureTemplatesAsync();
        return await CloneAsync($"{Prefix}_tpldemo");
    }

    /// <summary>База-шаблон с пустой схемой (сид выполнит сам тест).</summary>
    public static async Task<TestDatabase> CloneEmptyAsync()
    {
        await EnsureTemplatesAsync();
        return await CloneAsync($"{Prefix}_tpl");
    }

    private static async Task<TestDatabase> CloneAsync(string template)
    {
        var name = $"{Prefix}_{Guid.NewGuid():N}";
        await ExecuteAdminAsync($"CREATE DATABASE \"{name}\" TEMPLATE \"{template}\"");
        return new TestDatabase(name, ForDatabase(ServerConnectionString, name));
    }

    private static async Task EnsureTemplatesAsync()
    {
        await Gate.WaitAsync();
        try
        {
            if (templatesReady)
            {
                return;
            }

            // Идемпотентность: остатки от процесса с тем же pid (Windows переиспользует pid)
            // сносятся перед созданием шаблонов.
            await DropAllForPrefixAsync();

            await ExecuteAdminAsync(
                $"""CREATE DATABASE "{Prefix}_tpl" TEMPLATE template0 ENCODING 'UTF8' LC_COLLATE 'C' LC_CTYPE 'C'""");
            await ExecuteScriptAsync($"{Prefix}_tpl",
                await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "db", "02_schema.sql")));

            await ExecuteAdminAsync(
                $"""CREATE DATABASE "{Prefix}_tpldemo" TEMPLATE "{Prefix}_tpl" """);
            await SeedDemoTemplateAsync($"{Prefix}_tpldemo");

            templatesReady = true;
        }
        finally
        {
            Gate.Release();
        }
    }

    /// <summary>Наполняет шаблон демо-набором тем же сидом, что и приложение при старте.</summary>
    private static async Task SeedDemoTemplateAsync(string database)
    {
        var connectionString = ForDatabase(ServerConnectionString, database);
        await using var dataSource = NpgsqlDataSource.Create(WithNoPooling(connectionString));
        var store = new PostgresStore(dataSource);
        await using (store)
        {
            await store.OpenAsync();
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<MurketDbContext>()
                .UseNpgsql(store.Connection)
                .UseSnakeCaseNamingConvention()
                .Options;
            await using var db = new MurketDbContext(options);
            var seeder = new Seeder(db, new RawSqlOrderRepository(store),
                new PostgresUnitOfWork(store, db),
                new Pbkdf2PasswordHasher(210000, new KdfCounter(NullLogger<KdfCounter>.Instance)),
                new SeedOptions
                {
                    AdminPassword = "admin123",
                    CustomerPassword = "customer123",
                    DemoData = true,
                },
                NullLogger<Seeder>.Instance);
            await seeder.RunAsync(isDevelopment: true);
        }
    }

    private static async Task ExecuteAdminAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(WithNoPooling(ServerConnectionString));
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task ExecuteScriptAsync(string database, string script)
    {
        var connectionString = ForDatabase(ServerConnectionString, database);
        await using var connection = new NpgsqlConnection(WithNoPooling(connectionString));
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(script, connection);
        await command.ExecuteNonQueryAsync();
    }

    internal static async Task DropAsync(string name)
    {
        try
        {
            await ExecuteAdminAsync($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)");
        }
        catch (PostgresException)
        {
            // Форс-сброс недоступен (например, нет активных подключений — база уже удалена).
            await ExecuteAdminAsync($"DROP DATABASE IF EXISTS \"{name}\"");
        }
    }

    /// <summary>Сносит все тестовые базы этого процесса (шаблоны и оставшиеся клоны).</summary>
    private static void DropAllForPrefix() =>
        DropAllForPrefixAsync().GetAwaiter().GetResult();

    private static async Task DropAllForPrefixAsync()
    {
        var names = new List<string>();
        await using (var connection = new NpgsqlConnection(WithNoPooling(ServerConnectionString)))
        {
            await connection.OpenAsync();
            await using (var command = new NpgsqlCommand(
                "SELECT datname FROM pg_database WHERE datname LIKE @pattern", connection))
            {
                command.Parameters.AddWithValue("pattern", $"{Prefix}\\_%");
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    names.Add(reader.GetString(0));
                }
            }
        }

        foreach (var name in names)
        {
            await DropAsync(name);
        }
    }

    private static string ForDatabase(string connectionString, string database)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = database,
            // 23 зоны запускаются параллельными процессами: пул каждой зоны скромный,
            // чтобы суммарно не выйти за max_connections сервера.
            MaxPoolSize = 4,
        };
        return builder.ConnectionString;
    }

    private static string WithNoPooling(string connectionString)
    {
        // Пулы соединений держат базы живыми и мешают DROP DATABASE — в тестах пулинг не нужен.
        return new NpgsqlConnectionStringBuilder(connectionString) { Pooling = false }.ConnectionString;
    }
}

/// <summary>Тестовая база: имя и строка подключения; удаляется при Dispose.</summary>
public sealed class TestDatabase(string name, string connectionString) : IAsyncDisposable
{
    public string Name { get; } = name;

    public string ConnectionString { get; } = connectionString;

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await TestDatabases.DropAsync(Name);
    }
}
