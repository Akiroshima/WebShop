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
    public async Task<IReadOnlyList<CategoryDto>> CategoriesAsync()
    {
        // Количество товаров категории считаем по всем активным товарам.
        var all = await AllActiveProductsAsync();
        var counts = all.GroupBy(product => product.CategoryId)
            .ToDictionary(group => group.Key, group => group.Count());
        return [.. (await catalog.CategoriesAsync())
            .Select(category => new CategoryDto(category.Id, category.Name, counts.GetValueOrDefault(category.Id)))];
    }

    public async Task<IReadOnlyList<BrandDto>> BrandsAsync(string? categoryId)
    {
        var products = (await AllActiveProductsAsync())
            .Where(product => categoryId is null || product.CategoryId == categoryId)
            .Where(product => product.BrandId is not null)
            .GroupBy(product => product.BrandId!)
            .ToDictionary(group => group.Key, group => group.Count());
        return [.. (await catalog.BrandsAsync())
            .Where(brand => products.TryGetValue(brand.Id, out var count) && count > 0)
            .Select(brand => new BrandDto(brand.Id, brand.Name, products[brand.Id]))];
    }

    public async Task<PagedResult<ProductDto>> ProductsAsync(CatalogQuery query)
    {
        var result = await catalog.QueryProductsAsync(query);
        var (categoryNames, brandNames) = await NameMapsAsync();
        return new PagedResult<ProductDto>(
            [.. result.Items.Select(product => product.ToProductDto(categoryNames, brandNames))],
            result.Total, result.Page, result.PageSize);
    }

    public async Task<ProductDto> ProductAsync(string id)
    {
        var product = await catalog.FindProductAsync(id);
        if (product is null || !product.IsActive)
        {
            throw new NotFoundException("Товар не найден");
        }

        var (categoryNames, brandNames) = await NameMapsAsync();
        return product.ToProductDto(categoryNames, brandNames);
    }

    private async Task<List<Product>> AllActiveProductsAsync() =>
        [.. (await catalog.QueryProductsAsync(new CatalogQuery { Page = new PageRequest(1, 1_000_000) })).Items];

    private async Task<(Dictionary<string, string> Categories, Dictionary<string, string> Brands)> NameMapsAsync() =>
        ((await catalog.CategoriesAsync()).ToDictionary(category => category.Id, category => category.Name),
         (await catalog.BrandsAsync()).ToDictionary(brand => brand.Id, brand => brand.Name));

    /// <summary>Фильтр цены: «от» не больше «до».</summary>
    public static void ValidatePriceRange(decimal? min, decimal? max, ValidationRules.Errors errors)
    {
        if (min is > 0 && max is > 0 && min > max)
        {
            errors.Add("priceMin", "Минимальная цена не может превышать максимальную");
        }
    }
}
