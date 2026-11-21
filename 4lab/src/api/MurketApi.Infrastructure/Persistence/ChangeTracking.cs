using Microsoft.EntityFrameworkCore;

namespace MurketApi.Infrastructure.Persistence;

/// <summary>Добавляет сущность, только если она не отслеживается контекстом (иначе — обновление).</summary>
internal static class ChangeTracking
{
    public static void AddIfDetached<T>(this DbSet<T> set, T entity) where T : class
    {
        if (set.Entry(entity).State == EntityState.Detached)
        {
            set.Add(entity);
        }
    }
}
