using Microsoft.EntityFrameworkCore;

using MurketApi.Domain.Catalog;
using MurketApi.Domain.Cart;
using MurketApi.Domain.Common;
using MurketApi.Domain.Promos;
using MurketApi.Domain.Security;
using MurketApi.Domain.Users;

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

// =====================================================================
// Пользователи
// =====================================================================
public sealed class EfUserRepository(MurketDbContext db) : IUserRepository
{
    public Task<User?> FindByIdAsync(string id) => db.Users.FirstOrDefaultAsync(user => user.Id == id);

    public Task<User?> FindForLoginAsync(string login)
    {
        var trimmed = login.Trim();
        var lower = trimmed.ToLowerInvariant();
        var phone = Phone.Normalize(trimmed);
        return db.Users.FirstOrDefaultAsync(user =>
            user.Login.ToLower() == lower
            || user.Email.ToLower() == lower
            || (user.Phone != null && user.Phone == phone));
    }

    public Task<bool> EmailInUseAsync(string email, string? exceptUserId = null) =>
        db.Users.AnyAsync(user => user.Id != exceptUserId && user.Email == email);

    public Task<bool> PhoneInUseAsync(string phone, string? exceptUserId = null) =>
        db.Users.AnyAsync(user => user.Id != exceptUserId && user.Phone == phone);

    public async Task AddAsync(User user)
    {
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<User>> AllAsync() =>
        await db.Users.OrderBy(user => user.CreatedAt).ThenBy(user => user.Id).ToListAsync();
}

// =====================================================================
// Адреса доставки
// =====================================================================
public sealed class EfAddressRepository(MurketDbContext db) : IAddressRepository
{
    public async Task<IReadOnlyList<Address>> ForUserAsync(string userId) =>
        await db.Addresses
            .Where(address => address.UserId == userId)
            .OrderByDescending(address => address.IsDefault)
            .ThenBy(address => address.Label)
            .ToListAsync();

    public Task<Address?> FindForUserAsync(string userId, string addressId) =>
        db.Addresses.FirstOrDefaultAsync(address => address.Id == addressId && address.UserId == userId);

    public Task<int> CountForUserAsync(string userId) =>
        db.Addresses.CountAsync(address => address.UserId == userId);

    public async Task AddAsync(Address address)
    {
        db.Addresses.Add(address);
        await db.SaveChangesAsync();
    }

    public async Task RemoveAsync(Address address)
    {
        db.Addresses.Remove(address);
        await db.SaveChangesAsync();
    }
}

// =====================================================================
// Каталог: категории, бренды, товары
// =====================================================================
public sealed class EfCatalogRepository(MurketDbContext db) : ICatalogRepository
{
    public async Task<IReadOnlyList<Category>> CategoriesAsync() =>
        await db.Categories.OrderBy(category => category.Name).ToListAsync();

    public async Task<IReadOnlyList<Brand>> BrandsAsync() =>
        await db.Brands.OrderBy(brand => brand.Name).ToListAsync();

    public Task<Product?> FindProductAsync(string id) => db.Products.FirstOrDefaultAsync(product => product.Id == id);

    public Task<Product?> FindProductBySkuAsync(string sku)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        return db.Products.FirstOrDefaultAsync(product => product.Sku == normalized);
    }

    public Task<bool> SkuInUseAsync(string sku, string? exceptProductId = null)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        return db.Products.AnyAsync(product => product.Id != exceptProductId && product.Sku == normalized);
    }

    public Task<bool> CategoryExistsAsync(string categoryId) =>
        db.Categories.AnyAsync(category => category.Id == categoryId);

    public async Task AddProductAsync(Product product)
    {
        db.Products.Add(product);
        await db.SaveChangesAsync();
    }

    public async Task RemoveProductAsync(Product product)
    {
        db.Products.Remove(product);
        await db.SaveChangesAsync();
    }

    public async Task<PagedResult<Product>> QueryProductsAsync(CatalogQuery query)
    {
        var items = db.Products.AsQueryable();
        if (query.ActiveOnly)
        {
            items = items.Where(product => product.IsActive);
        }

        var search = query.Search?.Trim().ToLowerInvariant();
        if (!string.IsNullOrEmpty(search))
        {
            // Поиск по названию, артикулу и имени бренда (EXISTS по справочнику брендов).
            items = items.Where(product =>
                product.Name.ToLower().Contains(search)
                || product.Sku.ToLower().Contains(search)
                || db.Brands.Any(brand => brand.Id == product.BrandId && brand.Name.ToLower().Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(query.CategoryId))
        {
            items = items.Where(product => product.CategoryId == query.CategoryId);
        }

        if (query.BrandIds.Count > 0)
        {
            var brands = query.BrandIds;
            items = items.Where(product => product.BrandId != null && brands.Contains(product.BrandId));
        }

        if (query.PriceMin is > 0)
        {
            items = items.Where(product => product.Price >= query.PriceMin);
        }

        if (query.PriceMax is > 0)
        {
            items = items.Where(product => product.Price <= query.PriceMax);
        }

        if (query.OnlyAvailable)
        {
            items = items.Where(product => product.Stock > 0);
        }

        items = query.Sort switch
        {
            CatalogSort.Newest => items.OrderByDescending(product => product.CreatedAt),
            CatalogSort.PriceAsc => items.OrderBy(product => product.Price),
            CatalogSort.PriceDesc => items.OrderByDescending(product => product.Price),
            // Русская алфавитная сортировка — коллация базы; уточнение регистра не влияет на порядок.
            CatalogSort.NameAsc => items.OrderBy(product => product.Name),
            _ => items.OrderByDescending(product => product.IsPopular).ThenByDescending(product => product.Rating),
        };

        var total = await items.CountAsync();
        var pageItems = await items
            .Skip(query.Page.Skip)
            .Take(query.Page.PageSize)
            .ToListAsync();
        return new PagedResult<Product>(pageItems, total, query.Page.Page, query.Page.PageSize);
    }
}

// =====================================================================
// Корзины
// =====================================================================
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

// =====================================================================
// Промокоды
// =====================================================================
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

// =====================================================================
// Токены безопасности
// =====================================================================
public sealed class EfSecurityTokenRepository(MurketDbContext db) : ISecurityTokenRepository
{
    public async Task AddRefreshTokenAsync(RefreshToken token)
    {
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();
    }

    public Task<RefreshToken?> FindRefreshTokenAsync(string hash) =>
        db.RefreshTokens.FirstOrDefaultAsync(token => token.Hash == hash);

    public async Task<IReadOnlyList<RefreshToken>> RefreshTokensOfAsync(string userId) =>
        await db.RefreshTokens.Where(token => token.UserId == userId).ToListAsync();

    public Task RevokeRefreshTokenAsync(RefreshToken token, DateTimeOffset now)
    {
        token.Revoke(now);
        return db.SaveChangesAsync();
    }

    public async Task RevokeAllRefreshTokensAsync(string userId, DateTimeOffset now)
    {
        foreach (var token in await db.RefreshTokens.Where(token => token.UserId == userId).ToListAsync())
        {
            token.Revoke(now);
        }

        await db.SaveChangesAsync();
    }

    public async Task ReplaceRecoveryCodeAsync(string contact, RecoveryCode code)
    {
        db.RecoveryCodes.RemoveRange(
            await db.RecoveryCodes.Where(existing => existing.Contact == contact).ToListAsync());
        db.RecoveryCodes.Add(code);
        await db.SaveChangesAsync();
    }

    public Task<RecoveryCode?> FindLiveRecoveryCodeAsync(string contact) =>
        db.RecoveryCodes.FirstOrDefaultAsync(code => code.Contact == contact && !code.Used);

    public async Task SaveRecoveryCodeAsync(RecoveryCode code)
    {
        db.RecoveryCodes.AddIfDetached(code);
        await db.SaveChangesAsync();
    }

    public async Task AddResetTokenAsync(ResetToken token)
    {
        db.ResetTokens.Add(token);
        await db.SaveChangesAsync();
    }

    public async Task RevokeResetTokensAsync(string userId)
    {
        db.ResetTokens.RemoveRange(
            await db.ResetTokens.Where(token => token.UserId == userId).ToListAsync());
        await db.SaveChangesAsync();
    }

    public Task<ResetToken?> FindResetTokenAsync(string hash) =>
        db.ResetTokens.FirstOrDefaultAsync(token => token.Hash == hash);

    public async Task SaveResetTokenAsync(ResetToken token)
    {
        db.ResetTokens.AddIfDetached(token);
        await db.SaveChangesAsync();
    }
}
