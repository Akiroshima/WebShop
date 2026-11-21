using Microsoft.EntityFrameworkCore;

using MurketApi.Domain.Cart;

namespace MurketApi.Infrastructure.Persistence;

/// <summary>Репозиторий корзин на EF Core.</summary>
public sealed class EfCartRepository(MurketDbContext db) : ICartRepository
{
    public async Task<ShoppingCart?> FindByUserAsync(string userId)
    {
        var cart = await db.Carts.FirstOrDefaultAsync(cart => cart.UserId == userId);
        if (cart is null)
        {
            return null;
        }

        // Код применённого промокода хранится ссылкой (FK carts.promo_id) — восстанавливаем
        // снимок кода из справочника промокодов.
        if (cart.PromoId is null)
        {
            cart.RemovePromo();
            return cart;
        }

        var promo = await db.Promos.FindAsync(cart.PromoId);
        if (promo is null)
        {
            cart.PromoId = null;
            cart.RemovePromo();
        }
        else
        {
            cart.ApplyPromo(promo.Code);
        }

        return cart;
    }

    public async Task SaveAsync(ShoppingCart cart)
    {
        await SyncPromoReferenceAsync(cart);
        db.Carts.AddIfDetached(cart);
        await db.SaveChangesAsync();
    }

    /// <summary>Домен хранит код промокода, таблица — FK на promos: синхронизируем перед записью.</summary>
    private async Task SyncPromoReferenceAsync(ShoppingCart cart)
    {
        if (cart.PromoCode is null)
        {
            cart.PromoId = null;
            return;
        }

        var current = cart.PromoId is null
            ? null
            : await db.Promos.FindAsync(cart.PromoId);
        if (current is not null && current.Code == cart.PromoCode)
        {
            return;
        }

        var resolved = await db.Promos.FirstOrDefaultAsync(promo => promo.Code == cart.PromoCode);
        if (resolved is null)
        {
            // Промокод исчез из справочника — применённый код снимается.
            cart.RemovePromo();
            cart.PromoId = null;
        }
        else
        {
            cart.PromoId = resolved.Id;
        }
    }
}
