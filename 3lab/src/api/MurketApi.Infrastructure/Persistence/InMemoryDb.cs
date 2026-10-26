using MurketApi.Application.Abstractions;
using MurketApi.Domain.Cart;
using MurketApi.Domain.Catalog;
using MurketApi.Domain.Common;
using MurketApi.Domain.Orders;
using MurketApi.Domain.Promos;
using MurketApi.Domain.Security;
using MurketApi.Domain.Users;

namespace MurketApi.Infrastructure.Persistence;

/// <summary>
/// Заглушка БД: всё состояние процесса в памяти под одной блокировкой (Monitor реентерабелен,
/// поэтому вложенные захваты из единицы работы безопасны). При переходе на EF Core + PostgreSQL
/// класс заменяется DbContext-ом, а блокировка — транзакцией БД.
/// </summary>
public sealed class InMemoryDb
{
    public object Sync { get; } = new();

    public List<User> Users { get; } = [];

    public List<Category> Categories { get; } = [];

    public List<Brand> Brands { get; } = [];

    public List<Product> Products { get; } = [];

    public List<Address> Addresses { get; } = [];

    public List<PromoCode> Promos { get; } = [];

    public Dictionary<string, ShoppingCart> Carts { get; } = [];

    public List<Order> Orders { get; } = [];

    public int NextOrderNumber { get; set; } = 1001;

    public List<RefreshToken> RefreshTokens { get; } = [];

    public List<RecoveryCode> RecoveryCodes { get; } = [];

    public List<ResetToken> ResetTokens { get; } = [];
}

/// <summary>In-memory единица работы: атомарность через глобальную блокировку хранилища.</summary>
public sealed class InMemoryUnitOfWork(InMemoryDb db) : IUnitOfWork
{
    public T Execute<T>(Func<T> operation)
    {
        lock (db.Sync)
        {
            return operation();
        }
    }

    public void Execute(Action action)
    {
        lock (db.Sync)
        {
            action();
        }
    }
}
