namespace MurketApi.Domain.Promos;

/// <summary>Промокоды. Заглушка БД: InMemoryPromoRepository.</summary>
public interface IPromoRepository
{
    PromoCode? FindByCode(string code);

    void Save(PromoCode promo);
}
