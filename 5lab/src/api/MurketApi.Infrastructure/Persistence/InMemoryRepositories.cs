using MurketApi.Application.Abstractions;
using MurketApi.Domain.Cart;
using MurketApi.Domain.Catalog;
using MurketApi.Domain.Common;
using MurketApi.Domain.Orders;
using MurketApi.Domain.Promos;
using MurketApi.Domain.Security;
using MurketApi.Domain.Users;
using MurketApi.Infrastructure.Persistence;

namespace MurketApi.Infrastructure.Persistence;

public sealed class InMemoryUserRepository(InMemoryDb db) : IUserRepository
{
    public User? FindById(string id)
    {
        lock (db.Sync)
        {
            return db.Users.FirstOrDefault(user => user.Id == id);
        }
    }

    public User? FindForLogin(string login)
    {
        var trimmed = login.Trim();
        var phone = Phone.Normalize(trimmed);
        lock (db.Sync)
        {
            return db.Users.FirstOrDefault(user =>
                string.Equals(user.Login, trimmed, StringComparison.OrdinalIgnoreCase)
                || string.Equals(user.Email, trimmed, StringComparison.OrdinalIgnoreCase)
                || (user.Phone is not null && user.Phone == phone));
        }
    }

    public bool EmailInUse(string email, string? exceptUserId = null)
    {
        lock (db.Sync)
        {
            return db.Users.Any(user =>
                user.Id != exceptUserId
                && string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase));
        }
    }

    public bool PhoneInUse(string phone, string? exceptUserId = null)
    {
        lock (db.Sync)
        {
            return db.Users.Any(user => user.Id != exceptUserId && user.Phone == phone);
        }
    }

    public void Add(User user)
    {
        lock (db.Sync)
        {
            db.Users.Add(user);
        }
    }

    public IReadOnlyList<User> All()
    {
        lock (db.Sync)
        {
            return [.. db.Users];
        }
    }
}

public sealed class InMemoryAddressRepository(InMemoryDb db) : IAddressRepository
{
    public IReadOnlyList<Address> ForUser(string userId)
    {
        lock (db.Sync)
        {
            return [.. db.Addresses.Where(address => address.UserId == userId)];
        }
    }

    public Address? FindForUser(string userId, string addressId)
    {
        lock (db.Sync)
        {
            return db.Addresses.FirstOrDefault(address => address.Id == addressId && address.UserId == userId);
        }
    }

    public int CountForUser(string userId)
    {
        lock (db.Sync)
        {
            return db.Addresses.Count(address => address.UserId == userId);
        }
    }

    public void Add(Address address)
    {
        lock (db.Sync)
        {
            db.Addresses.Add(address);
        }
    }

    public void Remove(Address address)
    {
        lock (db.Sync)
        {
            db.Addresses.Remove(address);
        }
    }
}

public sealed class InMemoryCatalogRepository(InMemoryDb db) : ICatalogRepository
{
    public IReadOnlyList<Category> Categories()
    {
        lock (db.Sync)
        {
            return [.. db.Categories];
        }
    }

    public IReadOnlyList<Brand> Brands()
    {
        lock (db.Sync)
        {
            return [.. db.Brands];
        }
    }

    public Product? FindProduct(string id)
    {
        lock (db.Sync)
        {
            return db.Products.FirstOrDefault(product => product.Id == id);
        }
    }

    public Product? FindProductBySku(string sku)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        lock (db.Sync)
        {
            return db.Products.FirstOrDefault(product => product.Sku == normalized);
        }
    }

    public bool SkuInUse(string sku, string? exceptProductId = null)
    {
        var normalized = sku.Trim().ToUpperInvariant();
        lock (db.Sync)
        {
            return db.Products.Any(product => product.Id != exceptProductId && product.Sku == normalized);
        }
    }

    public bool CategoryExists(string categoryId)
    {
        lock (db.Sync)
        {
            return db.Categories.Any(category => category.Id == categoryId);
        }
    }

    public void AddProduct(Product product)
    {
        lock (db.Sync)
        {
            db.Products.Add(product);
        }
    }

    public void RemoveProduct(Product product)
    {
        lock (db.Sync)
        {
            db.Products.Remove(product);
        }
    }

    public PagedResult<Product> QueryProducts(CatalogQuery query)
    {
        lock (db.Sync)
        {
            IEnumerable<Product> items = db.Products;
            if (query.ActiveOnly)
            {
                items = items.Where(product => product.IsActive);
            }

            var search = query.Search?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(search))
            {
                items = items.Where(product =>
                {
                    var brandName = product.BrandId is null
                        ? string.Empty
                        : db.Brands.FirstOrDefault(brand => brand.Id == product.BrandId)?.Name ?? string.Empty;
                    return product.Name.ToLowerInvariant().Contains(search)
                        || product.Sku.ToLowerInvariant().Contains(search)
                        || brandName.ToLowerInvariant().Contains(search);
                });
            }

            if (!string.IsNullOrWhiteSpace(query.CategoryId))
            {
                items = items.Where(product => product.CategoryId == query.CategoryId);
            }

            if (query.BrandIds.Count > 0)
            {
                items = items.Where(product => product.BrandId is not null && query.BrandIds.Contains(product.BrandId));
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
                CatalogSort.NameAsc => items.OrderBy(product => product.Name, StringComparer.Create(GetRuCulture(), ignoreCase: false)),
                _ => items.OrderByDescending(product => product.IsPopular)
                    .ThenByDescending(product => product.Rating),
            };

            var materialized = items.ToList();
            var total = materialized.Count;
            var pageItems = materialized.Skip(query.Page.Skip).Take(query.Page.PageSize).ToList();
            return new PagedResult<Product>(pageItems, total, query.Page.Page, query.Page.PageSize);
        }
    }

    private static System.Globalization.CultureInfo GetRuCulture()
    {
        try
        {
            return System.Globalization.CultureInfo.GetCultureInfo("ru-RU");
        }
        catch (System.Globalization.CultureNotFoundException)
        {
            return System.Globalization.CultureInfo.InvariantCulture;
        }
    }
}

public sealed class InMemoryCartRepository(InMemoryDb db) : ICartRepository
{
    public ShoppingCart? FindByUser(string userId)
    {
        lock (db.Sync)
        {
            return db.Carts.GetValueOrDefault(userId);
        }
    }

    public void Save(ShoppingCart cart)
    {
        lock (db.Sync)
        {
            db.Carts[cart.UserId] = cart;
        }
    }
}

public sealed class InMemoryOrderRepository(InMemoryDb db) : IOrderRepository
{
    public void Add(Order order)
    {
        lock (db.Sync)
        {
            db.Orders.Add(order);
        }
    }

    public Order? FindById(string id)
    {
        lock (db.Sync)
        {
            return db.Orders.FirstOrDefault(order => order.Id == id);
        }
    }

    public PagedResult<Order> ForUser(string userId, PageRequest page)
    {
        lock (db.Sync)
        {
            var filtered = db.Orders
                .Where(order => order.UserId == userId)
                .OrderByDescending(order => order.CreatedAt)
                .ToList();
            return Page(filtered, page);
        }
    }

    public PagedResult<Order> All(OrderStatus? status, PageRequest page)
    {
        lock (db.Sync)
        {
            var filtered = db.Orders
                .Where(order => status is null || order.Status == status)
                .OrderByDescending(order => order.CreatedAt)
                .ToList();
            return Page(filtered, page);
        }
    }

    public int NextNumber()
    {
        lock (db.Sync)
        {
            return db.NextOrderNumber++;
        }
    }

    private static PagedResult<Order> Page(List<Order> orders, PageRequest page) =>
        new(orders.Skip(page.Skip).Take(page.PageSize).ToList(), orders.Count, page.Page, page.PageSize);
}

public sealed class InMemoryPromoRepository(InMemoryDb db) : IPromoRepository
{
    public PromoCode? FindByCode(string code)
    {
        var normalized = code.Trim().ToUpperInvariant();
        lock (db.Sync)
        {
            return db.Promos.FirstOrDefault(promo => promo.Code == normalized);
        }
    }

    public void Save(PromoCode promo)
    {
        lock (db.Sync)
        {
            var index = db.Promos.FindIndex(existing => existing.Id == promo.Id);
            if (index >= 0)
            {
                db.Promos[index] = promo;
            }
            else
            {
                db.Promos.Add(promo);
            }
        }
    }
}

public sealed class InMemorySecurityTokenRepository(InMemoryDb db) : ISecurityTokenRepository
{
    public void AddRefreshToken(RefreshToken token)
    {
        lock (db.Sync)
        {
            db.RefreshTokens.Add(token);
        }
    }

    public RefreshToken? FindRefreshToken(string hash)
    {
        lock (db.Sync)
        {
            return db.RefreshTokens.FirstOrDefault(token => token.Hash == hash);
        }
    }

    public IReadOnlyList<RefreshToken> RefreshTokensOf(string userId)
    {
        lock (db.Sync)
        {
            return [.. db.RefreshTokens.Where(token => token.UserId == userId)];
        }
    }

    public void RevokeRefreshToken(RefreshToken token, DateTimeOffset now)
    {
        lock (db.Sync)
        {
            token.Revoke(now);
        }
    }

    public void RevokeAllRefreshTokens(string userId, DateTimeOffset now)
    {
        lock (db.Sync)
        {
            foreach (var token in db.RefreshTokens.Where(token => token.UserId == userId))
            {
                token.Revoke(now);
            }
        }
    }

    public void ReplaceRecoveryCode(string contact, RecoveryCode code)
    {
        lock (db.Sync)
        {
            db.RecoveryCodes.RemoveAll(existing => existing.Contact == contact);
            db.RecoveryCodes.Add(code);
        }
    }

    public RecoveryCode? FindLiveRecoveryCode(string contact)
    {
        lock (db.Sync)
        {
            return db.RecoveryCodes.FirstOrDefault(existing => existing.Contact == contact && !existing.Used);
        }
    }

    public void SaveRecoveryCode(RecoveryCode code)
    {
        lock (db.Sync)
        {
            var index = db.RecoveryCodes.FindIndex(existing => existing.Id == code.Id);
            if (index >= 0)
            {
                db.RecoveryCodes[index] = code;
            }
            else
            {
                db.RecoveryCodes.Add(code);
            }
        }
    }

    public void AddResetToken(ResetToken token)
    {
        lock (db.Sync)
        {
            db.ResetTokens.Add(token);
        }
    }

    public void RevokeResetTokens(string userId)
    {
        lock (db.Sync)
        {
            db.ResetTokens.RemoveAll(token => token.UserId == userId);
        }
    }

    public ResetToken? FindResetToken(string hash)
    {
        lock (db.Sync)
        {
            return db.ResetTokens.FirstOrDefault(token => token.Hash == hash);
        }
    }

    public void SaveResetToken(ResetToken token)
    {
        lock (db.Sync)
        {
            var index = db.ResetTokens.FindIndex(existing => existing.Hash == token.Hash);
            if (index >= 0)
            {
                db.ResetTokens[index] = token;
            }
            else
            {
                db.ResetTokens.Add(token);
            }
        }
    }
}
