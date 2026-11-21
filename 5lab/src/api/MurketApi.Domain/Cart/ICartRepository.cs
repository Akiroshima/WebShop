namespace MurketApi.Domain.Cart;

/// <summary>Корзины. Заглушка БД: InMemoryCartRepository.</summary>
public interface ICartRepository
{
    ShoppingCart? FindByUser(string userId);

    void Save(ShoppingCart cart);
}
