using MurketApi.Domain.Common;

namespace MurketApi.Domain.Catalog;

/// <summary>Каталог: категории, бренды, товары. Реализация — EF Core (PostgreSQL).</summary>
public interface ICatalogRepository
{
    Task<IReadOnlyList<Category>> CategoriesAsync();

    Task<IReadOnlyList<Brand>> BrandsAsync();

    Task<Product?> FindProductAsync(string id);

    Task<Product?> FindProductBySkuAsync(string sku);

    Task<bool> SkuInUseAsync(string sku, string? exceptProductId = null);

    Task<bool> CategoryExistsAsync(string categoryId);

    Task AddProductAsync(Product product);

    Task RemoveProductAsync(Product product);

    /// <summary>Товарная выборка с фильтрами, сортировкой и постраничной выдачей.</summary>
    Task<PagedResult<Product>> QueryProductsAsync(CatalogQuery query);
}
