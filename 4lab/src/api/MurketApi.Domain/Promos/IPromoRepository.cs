namespace MurketApi.Domain.Promos;

/// <summary>Промокоды. Реализация — EF Core (PostgreSQL).</summary>
public interface IPromoRepository
{
    Task<PromoCode?> FindByCodeAsync(string code);

    Task SaveAsync(PromoCode promo);
}
