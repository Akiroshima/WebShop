namespace MurketApi.Domain.Cart;

/// <summary>Корзины. Реализация — EF Core (PostgreSQL).</summary>
public interface ICartRepository
{
    Task<ShoppingCart?> FindByUserAsync(string userId);

    Task SaveAsync(ShoppingCart cart);
}
