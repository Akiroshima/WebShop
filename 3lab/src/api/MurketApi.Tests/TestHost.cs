using Microsoft.Extensions.Logging.Abstractions;

using MurketApi.Application.Abstractions;
using MurketApi.Application.Services;
using MurketApi.Domain.Common;
using MurketApi.Infrastructure;
using MurketApi.Infrastructure.Metrics;
using MurketApi.Infrastructure.Persistence;
using MurketApi.Infrastructure.RateLimiting;
using MurketApi.Infrastructure.Security;
using MurketApi.Infrastructure.Seeding;

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

/// <summary>Композиция приложения на заглушках БД: сервисы получают реальные зависимости, время управляемое.</summary>
public sealed class TestHost
{
    public FakeClock Clock { get; } = new();

    public InMemoryDb Db { get; } = new();

    public SpyEmailSender Emails { get; } = new();

    public SpySmsSender Sms { get; } = new();

    public SlidingWindowRateLimiter RateLimiter { get; } = new();

    public IPasswordHasher Hasher { get; }

    public ITokenHasher TokenHasher { get; } = new SaltedSha256TokenHasher();

    public AuthService Auth { get; }

    public CatalogService Catalog { get; }

    public CartService Cart { get; }

    public OrderService Orders { get; }

    public ProfileService Profile { get; }

    public AdminService Admin { get; }

    public TestHost(bool seedDemoData = true)
    {
        var unitOfWork = new InMemoryUnitOfWork(Db);
        Hasher = new Pbkdf2PasswordHasher(iterations: 1_000, new KdfCounter(NullLogger<KdfCounter>.Instance));
        var users = new InMemoryUserRepository(Db);
        var addresses = new InMemoryAddressRepository(Db);
        var catalogRepo = new InMemoryCatalogRepository(Db);
        var carts = new InMemoryCartRepository(Db);
        var orders = new InMemoryOrderRepository(Db);
        var promos = new InMemoryPromoRepository(Db);
        var tokens = new InMemorySecurityTokenRepository(Db);

        Auth = new AuthService(users, tokens, Hasher,
            new JwtAccessTokenService(new string('k', 64), TimeSpan.FromMinutes(15)),
            new CryptoSecretGenerator(), TokenHasher, Emails, Sms, RateLimiter,
            new KdfCounter(NullLogger<KdfCounter>.Instance), Clock);
        Catalog = new CatalogService(catalogRepo);
        Cart = new CartService(carts, catalogRepo, promos, unitOfWork, Clock);
        Orders = new OrderService(orders, catalogRepo, carts, promos, addresses, unitOfWork, Clock);
        Profile = new ProfileService(users, addresses, tokens, Hasher, TokenHasher,
            new KdfCounter(NullLogger<KdfCounter>.Instance), Clock, unitOfWork);
        Admin = new AdminService(catalogRepo, users, carts, unitOfWork, Clock);

        new Seeder(Db, Hasher, new SeedOptions
        {
            AdminPassword = "admin123",
            CustomerPassword = "customer123",
            DemoData = seedDemoData,
        }, NullLogger<Seeder>.Instance).Run(isDevelopment: true);
    }

    public const string CustomerId = DemoData.CustomerId;

    public const string AdminId = DemoData.AdminId;
}
