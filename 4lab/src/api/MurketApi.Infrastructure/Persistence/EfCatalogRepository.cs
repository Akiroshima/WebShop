using Microsoft.EntityFrameworkCore;

using MurketApi.Domain.Catalog;
using MurketApi.Domain.Common;

namespace MurketApi.Infrastructure.Persistence;

/// <summary>Репозиторий каталога (категории, бренды, товары) на EF Core.</summary>
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
