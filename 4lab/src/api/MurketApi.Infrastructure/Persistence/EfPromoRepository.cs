using Microsoft.EntityFrameworkCore;

using MurketApi.Domain.Promos;

namespace MurketApi.Infrastructure.Persistence;

/// <summary>Репозиторий промокодов на EF Core.</summary>
public sealed class EfPromoRepository(MurketDbContext db) : IPromoRepository
{
    public Task<PromoCode?> FindByCodeAsync(string code)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return db.Promos.FirstOrDefaultAsync(promo => promo.Code == normalized);
    }

    public async Task SaveAsync(PromoCode promo)
    {
        db.Promos.AddIfDetached(promo);
        await db.SaveChangesAsync();
    }
}
