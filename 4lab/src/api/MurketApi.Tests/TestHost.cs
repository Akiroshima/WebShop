using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using MurketApi.Application.Abstractions;
using MurketApi.Application.Services;
using MurketApi.Domain.Common;
using MurketApi.Infrastructure.Metrics;
using MurketApi.Infrastructure.Persistence;
using MurketApi.Infrastructure.RateLimiting;
using MurketApi.Infrastructure.Security;
using MurketApi.Infrastructure.Seeding;
using MurketApi.TestKit;

using Npgsql;

namespace MurketApi.Tests;

/// <summary>Управляемые часы: тесты двигают время явным образом.</summary>
public sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    public void Advance(TimeSpan span) => UtcNow += span;
}

/// <summary>Захват «отправленных» писем и SMS: тесты читают код восстановления.</summary>
public sealed class SpyEmailSender : IEmailSender
{
    public List<(string To, string Code)> Sent { get; } = [];

    public void SendRecoveryCode(string to, string code) => Sent.Add((to, code));
}

public sealed class SpySmsSender : ISmsSender
{
    public List<(string To, string Code)> Sent { get; } = [];

    public void SendRecoveryCode(string to, string code) => Sent.Add((to, code));
}

/// <summary>
/// Композиция сервисов на реальной PostgreSQL: каждый тест получает собственную базу (клон шаблона
/// из TestKit) и работает с настоящими репозиториями — EF Core и «сырым» SQL. Время управляемое,
/// письма/SMS перехватываются, PBKDF2 ускорен до 1000 итераций.
/// </summary>
public sealed class TestHost : IAsyncDisposable
{
    private readonly TestDatabase database;

    private readonly NpgsqlDataSource dataSource;

    private readonly PostgresStore store;

    private TestHost(TestDatabase database, NpgsqlDataSource dataSource, PostgresStore store,
        MurketDbContext db, FakeClock clock, IPasswordHasher hasher, SpyEmailSender emails,
        SpySmsSender sms, SlidingWindowRateLimiter rateLimiter, AuthService auth, CatalogService catalog,
        CartService cart, OrderService orders, ProfileService profile, AdminService admin)
    {
        this.database = database;
        this.dataSource = dataSource;
        this.store = store;
        Db = db;
        Clock = clock;
        Hasher = hasher;
        Emails = emails;
        Sms = sms;
        RateLimiter = rateLimiter;
        Auth = auth;
        Catalog = catalog;
        Cart = cart;
        Orders = orders;
        Profile = profile;
        Admin = admin;
    }

    public FakeClock Clock { get; }

    public MurketDbContext Db { get; }

    public SpyEmailSender Emails { get; }

    public SpySmsSender Sms { get; }

    public SlidingWindowRateLimiter RateLimiter { get; }

    public IPasswordHasher Hasher { get; }

    public AuthService Auth { get; }

    public CatalogService Catalog { get; }

    public CartService Cart { get; }

    public OrderService Orders { get; }

    public ProfileService Profile { get; }

    public AdminService Admin { get; }

    public static async Task<TestHost> CreateAsync(bool seedDemoData = true)
    {
        // Клон шаблона с демо-данными — если демо не нужно, клонируется пустая схема
        // и сид создаёт только пользователей и каталог.
        var database = seedDemoData
            ? await TestDatabases.CloneDemoAsync()
            : await TestDatabases.CloneEmptyAsync();

        var builder = new NpgsqlConnectionStringBuilder(database.ConnectionString) { Pooling = false };
        var dataSource = NpgsqlDataSource.Create(builder.ConnectionString);
        var store = new PostgresStore(dataSource);
        var db = new MurketDbContext(new DbContextOptionsBuilder<MurketDbContext>()
            .UseNpgsql(store.Connection)
            .UseSnakeCaseNamingConvention()
            .Options);

        var emails = new SpyEmailSender();
        var sms = new SpySmsSender();
        var rateLimiter = new SlidingWindowRateLimiter();
        var clock = new FakeClock();
        var kdf = new KdfCounter(NullLogger<KdfCounter>.Instance);
        var hasher = new Pbkdf2PasswordHasher(iterations: 1_000, kdf);
        var tokenHasher = new SaltedSha256TokenHasher("unit-tests-pepper");

        var users = new EfUserRepository(db);
        var addresses = new EfAddressRepository(db);
        var catalogRepo = new EfCatalogRepository(db);
        var carts = new EfCartRepository(db);
        var orderRepo = new RawSqlOrderRepository(store);
        var promos = new EfPromoRepository(db);
        var tokens = new EfSecurityTokenRepository(db);
        var unitOfWork = new PostgresUnitOfWork(store, db);

        var auth = new AuthService(users, tokens, hasher,
            new JwtAccessTokenService(new string('k', 64), TimeSpan.FromMinutes(15)),
            new CryptoSecretGenerator(), tokenHasher, emails, sms, rateLimiter, kdf, clock, unitOfWork);
        var catalog = new CatalogService(catalogRepo);
        var cart = new CartService(carts, catalogRepo, promos, unitOfWork, clock);
        var orders = new OrderService(orderRepo, catalogRepo, carts, promos, addresses, unitOfWork, clock);
        var profile = new ProfileService(users, addresses, tokens, hasher, tokenHasher, kdf, clock, unitOfWork);
        var admin = new AdminService(catalogRepo, users, carts, unitOfWork, clock);

        var host = new TestHost(database, dataSource, store, db, clock, hasher, emails, sms, rateLimiter,
            auth, catalog, cart, orders, profile, admin);

        await new Seeder(db, orderRepo, unitOfWork, hasher, new SeedOptions
        {
            AdminPassword = "admin123",
            CustomerPassword = "customer123",
            DemoData = seedDemoData,
        }, NullLogger<Seeder>.Instance).RunAsync(isDevelopment: true);
        return host;
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await Db.DisposeAsync();
        await store.DisposeAsync();
        await dataSource.DisposeAsync();
        await database.DisposeAsync();
    }

    public const string CustomerId = DemoData.CustomerId;

    public const string AdminId = DemoData.AdminId;
}
