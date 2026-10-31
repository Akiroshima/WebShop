using System.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

using MurketApi.Domain.Catalog;
using MurketApi.Domain.Cart;
using MurketApi.Domain.Common;
using MurketApi.Domain.Promos;
using MurketApi.Domain.Security;
using MurketApi.Domain.Users;

namespace MurketApi.Infrastructure.Persistence;

/// <summary>
/// Соединение текущего scope-а: один NpgsqlConnection на запрос, которым пользуются и EF Core
/// (DbContext привязан к Connection), и репозиторий заказов на «сыром» ADO.NET. Единица работы
/// открывает на этом же соединении транзакцию — обе технологии работают атомарно.
/// </summary>
public sealed class PostgresStore(Npgsql.NpgsqlDataSource dataSource) : IAsyncDisposable, IDisposable
{
    private Npgsql.NpgsqlConnection? connection;

    public Npgsql.NpgsqlConnection Connection => connection ??= dataSource.CreateConnection();

    /// <summary>Активная транзакция единицы работы (null — вне единицы работы).</summary>
    public Npgsql.NpgsqlTransaction? Transaction { get; set; }

    public async Task<Npgsql.NpgsqlConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        if (Connection.State != ConnectionState.Open)
        {
            await Connection.OpenAsync(cancellationToken);
        }

        return Connection;
    }

    public ValueTask DisposeAsync()
    {
        Transaction = null;
        return connection?.DisposeAsync() ?? ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        Transaction = null;
        connection?.Dispose();
    }
}

/// <summary>Точка входа EF Core: доменные сущности отображаются в схему src/api/db/02_schema.sql.</summary>
public sealed class MurketDbContext(DbContextOptions<MurketDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Brand> Brands => Set<Brand>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Address> Addresses => Set<Address>();

    public DbSet<PromoCode> Promos => Set<PromoCode>();

    public DbSet<ShoppingCart> Carts => Set<ShoppingCart>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<RecoveryCode> RecoveryCodes => Set<RecoveryCode>();

    public DbSet<ResetToken> ResetTokens => Set<ResetToken>();

    // Конвертеры идентификаторов: домен работает со строками, PostgreSQL хранит uuid.
    // Некорректная строка (не uuid) читается как Guid.Empty и не совпадает ни с одной строкой —
    // пользовательские id из запросов не ломают SQL, а дают привычные 404/400.
    private static readonly ValueConverter<string, Guid> UuidConverter =
        new(id => ParseId(id), guid => guid.ToString("D"));

    private static readonly ValueComparer<string> UuidComparer =
        new((a, b) => a == b, id => id.GetHashCode());

    private static readonly ValueConverter<string?, Guid> NullableUuidConverter =
        new(id => ParseId(id), guid => guid.ToString("D"));

    private static readonly ValueComparer<string?> NullableUuidComparer =
        new((a, b) => a == b, id => id == null ? 0 : id.GetHashCode());

    private static Guid ParseId(string? id) =>
        Guid.TryParse(id, out var value) ? value : Guid.Empty;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Индексы и CHECK-ограничения описаны в SQL-скриптах (src/api/db/02_schema.sql):
        // здесь — только маппинг таблиц/колонок и внешних ключей (snake_case включён в Program).
        ConfigureUsers(modelBuilder.Entity<User>());
        ConfigureCatalog(modelBuilder.Entity<Category>(), modelBuilder.Entity<Brand>(),
            modelBuilder.Entity<Product>());
        ConfigureAddresses(modelBuilder.Entity<Address>());
        ConfigurePromos(modelBuilder.Entity<PromoCode>());
        ConfigureCarts(modelBuilder.Entity<ShoppingCart>());
        ConfigureSecurity(modelBuilder.Entity<RefreshToken>(), modelBuilder.Entity<RecoveryCode>(),
            modelBuilder.Entity<ResetToken>());
    }

    private static void ConfigureUsers(EntityTypeBuilder<User> users)
    {
        users.ToTable("users");
        users.HasKey(user => user.Id);
        users.Property(user => user.Id).HasConversion(UuidConverter, UuidComparer);
        users.Property(user => user.Login).HasColumnName("login");
        users.Property(user => user.Role)
            .HasConversion(role => role == UserRole.Admin ? "admin" : "customer",
                value => value == "admin" ? UserRole.Admin : UserRole.Customer);
    }

    private static void ConfigureCatalog(EntityTypeBuilder<Category> categories,
        EntityTypeBuilder<Brand> brands, EntityTypeBuilder<Product> products)
    {
        categories.ToTable("categories");
        categories.HasKey(category => category.Id);
        categories.Property(category => category.Id).HasConversion(UuidConverter, UuidComparer);
        categories.HasIndex(category => category.Name).IsUnique();

        brands.ToTable("brands");
        brands.HasKey(brand => brand.Id);
        brands.Property(brand => brand.Id).HasConversion(UuidConverter, UuidComparer);
        brands.HasIndex(brand => brand.Name).IsUnique();

        products.ToTable("products");
        products.HasKey(product => product.Id);
        products.Property(product => product.Id).HasConversion(UuidConverter, UuidComparer);
        products.Property(product => product.Price).HasColumnType("numeric(12,2)");
        products.Property(product => product.BrandId)
            .HasConversion(NullableUuidConverter, NullableUuidComparer);
        products.HasOne<Brand>()
            .WithMany()
            .HasForeignKey(product => product.BrandId)
            .OnDelete(DeleteBehavior.SetNull);
        products.HasOne<Category>()
            .WithMany()
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureAddresses(EntityTypeBuilder<Address> addresses)
    {
        addresses.ToTable("addresses");
        addresses.HasKey(address => address.Id);
        addresses.Property(address => address.Id).HasConversion(UuidConverter, UuidComparer);
        addresses.HasOne<User>()
            .WithMany()
            .HasForeignKey(address => address.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurePromos(EntityTypeBuilder<PromoCode> promos)
    {
        promos.ToTable("promos");
        promos.HasKey(promo => promo.Id);
        promos.Property(promo => promo.Id).HasConversion(UuidConverter, UuidComparer);
        promos.Property(promo => promo.Value).HasColumnType("numeric(12,2)");
        promos.Property(promo => promo.MinSubtotal).HasColumnType("numeric(12,2)");
        promos.Property(promo => promo.Type)
            .HasConversion(type => type == PromoType.Percent ? "percent" : "fixed",
                value => value == "percent" ? PromoType.Percent : PromoType.Fixed);
    }

    private static void ConfigureCarts(EntityTypeBuilder<ShoppingCart> carts)
    {
        carts.ToTable("carts");
        carts.HasKey(cart => cart.UserId);
        // Домен держит код промокода (снимок), в базе — ссылка promos.promo_id:
        // репозиторий корзины синхронизирует PromoCode ↔ PromoId при чтении и записи.
        carts.Ignore(cart => cart.PromoCode);
        carts.Property(cart => cart.UserId).HasConversion(UuidConverter, UuidComparer);
        carts.Property(cart => cart.PromoId)
            .HasConversion(NullableUuidConverter, NullableUuidComparer);
        carts.HasOne<PromoCode>()
            .WithMany()
            .HasForeignKey(cart => cart.PromoId)
            .OnDelete(DeleteBehavior.SetNull);
        carts.HasOne<User>()
            .WithMany()
            .HasForeignKey(cart => cart.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        carts.OwnsMany(cart => cart.Items, items =>
        {
            items.ToTable("cart_items");
            items.WithOwner().HasForeignKey("CartUserId");
            items.HasKey("CartUserId", nameof(CartItem.ProductId));
            items.Property("CartUserId").HasConversion(UuidConverter, UuidComparer)
                .HasColumnName("cart_user_id");
            items.Property(item => item.ProductId).HasConversion(UuidConverter, UuidComparer);
            items.HasOne<Product>()
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureSecurity(EntityTypeBuilder<RefreshToken> refresh,
        EntityTypeBuilder<RecoveryCode> recovery, EntityTypeBuilder<ResetToken> reset)
    {
        refresh.ToTable("refresh_tokens");
        refresh.HasKey(token => token.Id);
        refresh.Property(token => token.Hash).HasColumnName("token_hash");
        refresh.HasIndex(token => token.Hash).IsUnique();
        refresh.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        recovery.ToTable("recovery_codes");
        recovery.HasKey(code => code.Id);
        recovery.Property(code => code.Id).HasConversion(UuidConverter, UuidComparer);
        recovery.Property(code => code.Channel)
            .HasConversion(channel => channel == RecoveryChannel.Email ? "email" : "phone",
                value => value == "email" ? RecoveryChannel.Email : RecoveryChannel.Phone);
        recovery.HasOne<User>()
            .WithMany()
            .HasForeignKey(code => code.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        reset.ToTable("password_reset_tokens");
        reset.HasKey(token => token.Hash);
        reset.Property(token => token.Hash).HasColumnName("token_hash");
        reset.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
