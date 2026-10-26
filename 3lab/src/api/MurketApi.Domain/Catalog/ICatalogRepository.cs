using MurketApi.Domain.Common;

namespace MurketApi.Domain.Catalog;

/// <summary>Каталог: категории, бренды, товары. Заглушка БД: InMemoryCatalogRepository.</summary>
public interface ICatalogRepository
{
    IReadOnlyList<Category> Categories();

    IReadOnlyList<Brand> Brands();

    Product? FindProduct(string id);

    Product? FindProductBySku(string sku);

    bool SkuInUse(string sku, string? exceptProductId = null);

    bool CategoryExists(string categoryId);

    void AddProduct(Product product);

    void RemoveProduct(Product product);

    /// <summary>Товарная выборка с фильтрами, сортировкой и постраничной выдачей.</summary>
    PagedResult<Product> QueryProducts(CatalogQuery query);
}
