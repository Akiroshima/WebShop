using MurketApi.Application.Abstractions;
using MurketApi.Application.Dtos;
using MurketApi.Application.Mapping;
using MurketApi.Application.Validation;
using MurketApi.Domain.Catalog;
using MurketApi.Domain.Common;
using MurketApi.Domain.Users;

namespace MurketApi.Application.Services;

/// <summary>Каталог: категории и бренды с количеством товаров, постраничный список, карточка товара.</summary>
public sealed class CatalogService(ICatalogRepository catalog)
{
    public IReadOnlyList<CategoryDto> Categories()
    {
        // Количество товаров категории считаем по всем активным товарам.
        var all = AllActiveProducts();
        var counts = all.GroupBy(product => product.CategoryId)
            .ToDictionary(group => group.Key, group => group.Count());
        return [.. catalog.Categories()
            .Select(category => new CategoryDto(category.Id, category.Name, counts.GetValueOrDefault(category.Id)))];
    }

    public IReadOnlyList<BrandDto> Brands(string? categoryId)
    {
        var products = AllActiveProducts()
            .Where(product => categoryId is null || product.CategoryId == categoryId)
            .Where(product => product.BrandId is not null)
            .GroupBy(product => product.BrandId!)
            .ToDictionary(group => group.Key, group => group.Count());
        return [.. catalog.Brands()
            .Where(brand => products.TryGetValue(brand.Id, out var count) && count > 0)
            .Select(brand => new BrandDto(brand.Id, brand.Name, products[brand.Id]))];
    }

    public PagedResult<ProductDto> Products(CatalogQuery query)
    {
        var result = catalog.QueryProducts(query);
        var (categoryNames, brandNames) = NameMaps();
        return new PagedResult<ProductDto>(
            [.. result.Items.Select(product => product.ToProductDto(categoryNames, brandNames))],
            result.Total,
            result.Page,
            result.PageSize);
    }

    public ProductDto Product(string id)
    {
        var product = catalog.FindProduct(id);
        if (product is null || !product.IsActive)
        {
            throw new NotFoundException("Товар не найден");
        }

        var (categoryNames, brandNames) = NameMaps();
        return product.ToProductDto(categoryNames, brandNames);
    }

    private List<Product> AllActiveProducts() =>
        [.. catalog.QueryProducts(new CatalogQuery { Page = new PageRequest(1, 1_000_000) }).Items];

    private (Dictionary<string, string> Categories, Dictionary<string, string> Brands) NameMaps() =>
        (catalog.Categories().ToDictionary(category => category.Id, category => category.Name),
         catalog.Brands().ToDictionary(brand => brand.Id, brand => brand.Name));

    /// <summary>Фильтр цены: «от» не больше «до».</summary>
    public static void ValidatePriceRange(decimal? min, decimal? max, ValidationRules.Errors errors)
    {
        if (min is > 0 && max is > 0 && min > max)
        {
            errors.Add("priceMin", "Минимальная цена не может превышать максимальную");
        }
    }
}
