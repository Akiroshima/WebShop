using Microsoft.EntityFrameworkCore;

using MurketApi.Application.Abstractions;

namespace MurketApi.Infrastructure.Persistence;

/// <summary>
/// Единица работы на PostgreSQL: операции выполняются в одной транзакции на соединении scope-а.
/// DbContext и репозиторий заказов (сырой ADO.NET) работают с этой транзакцией совместно,
/// поэтому оформление заказа атомарно: списание остатков, рост used_count промокода, очистка
/// корзины и вставка заказа commit-ятся вместе. Финальный SaveChanges фиксирует изменения
/// отслеживаемых EF Core сущностей, которые сервисы изменили без явного вызова репозитория.
/// </summary>
public sealed class PostgresUnitOfWork(PostgresStore store, MurketDbContext db) : IUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        await store.OpenAsync();
        if (store.Transaction is not null)
        {
            // Вложенная единица работы присоединяется к уже открытой транзакции.
            return await operation();
        }

        var transaction = await store.Connection.BeginTransactionAsync();
        store.Transaction = transaction;
        T result;
        try
        {
            await db.Database.UseTransactionAsync(transaction);
            result = await operation();
            await db.SaveChangesAsync();
        }
        catch
        {
            await DetachAndRollbackAsync(transaction);
            throw;
        }

        await db.Database.UseTransactionAsync(null);
        await transaction.CommitAsync();
        await transaction.DisposeAsync();
        if (ReferenceEquals(store.Transaction, transaction))
        {
            store.Transaction = null;
        }

        return result;
    }

    public async Task ExecuteAsync(Func<Task> operation)
    {
        await ExecuteAsync(async () =>
        {
            await operation();
            return true;
        });
    }

    private async Task DetachAndRollbackAsync(Npgsql.NpgsqlTransaction transaction)
    {
        try
        {
            await db.Database.UseTransactionAsync(null);
        }
        catch (Exception)
        {
            // Откат важнее: EF Core мог не успеть привязать транзакцию.
        }

        try
        {
            await transaction.RollbackAsync();
        }
        catch (Exception)
        {
            // Соединение могло оборваться — транзакция умрёт вместе с ним.
        }

        await transaction.DisposeAsync();
        if (ReferenceEquals(store.Transaction, transaction))
        {
            store.Transaction = null;
        }
    }
}
