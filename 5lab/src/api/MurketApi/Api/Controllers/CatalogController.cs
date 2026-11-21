using Microsoft.AspNetCore.Mvc;

using MurketApi.Application.Dtos;
using MurketApi.Application.Services;
using MurketApi.Application.Validation;
using MurketApi.Domain.Catalog;
using MurketApi.Domain.Common;

namespace MurketApi.Api.Controllers;

/// <summary>Каталог: категории, бренды, постраничный список товаров и карточка.</summary>
[ApiController]
[Route("api/v1/catalog")]
public sealed class CatalogController(CatalogService catalog) : ControllerBase
{
    /// <summary>Категории с количеством активных товаров.</summary>
    [HttpGet("categories")]
    public ActionResult<IReadOnlyList<CategoryDto>> Categories() => Ok(catalog.Categories());

    /// <summary>Бренды с количеством товаров (учитывает выбранную категорию).</summary>
    [HttpGet("brands")]
    public ActionResult<IReadOnlyList<BrandDto>> Brands([FromQuery] string? category) => Ok(catalog.Brands(category));

    /// <summary>Товары: фильтры (search, category, brand*, priceMin, priceMax, available), сортировка, страница.</summary>
    [HttpGet("products")]
    public ActionResult<PagedResult<ProductDto>> Products(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] List<string>? brand,
        [FromQuery] string? priceMin,
        [FromQuery] string? priceMax,
        [FromQuery] string? available,
        [FromQuery] string? sort,
        [FromQuery] string? page,
        [FromQuery] string? pageSize)
    {
        var errors = new ValidationRules.Errors();
        var min = ParsePrice(priceMin, "priceMin", errors);
        var max = ParsePrice(priceMax, "priceMax", errors);
        if (min is > 0 && max is > 0 && min > max)
        {
            errors.Add("priceMin", "Минимальная цена не может превышать максимальную");
        }

        if (available is not null && available is not "true" and not "false")
        {
            errors.Add("available", "Допустимые значения: true, false");
        }

        var parsedSort = CatalogSort.Popular;
        if (!string.IsNullOrWhiteSpace(sort))
        {
            parsedSort = sort switch
            {
                "popular" => CatalogSort.Popular,
                "newest" => CatalogSort.Newest,
                "price-asc" => CatalogSort.PriceAsc,
                "price-desc" => CatalogSort.PriceDesc,
                "name-asc" => CatalogSort.NameAsc,
                _ => throw new ValidationException("Данные заполнены неверно",
                    new Dictionary<string, string[]> { ["sort"] = ["Неизвестная сортировка"] }),
            };
        }

        var request = PageRequest.Normalize(ParseInt(page, "page", errors), ParseInt(pageSize, "pageSize", errors));
        if (errors.Any)
        {
            throw errors.ToException();
        }

        var query = new CatalogQuery
        {
            Search = search,
            CategoryId = category,
            BrandIds = brand ?? [],
            PriceMin = min,
            PriceMax = max,
            OnlyAvailable = available == "true",
            Sort = parsedSort,
            ActiveOnly = true,
            Page = request,
        };
        return Ok(catalog.Products(query));
    }

    /// <summary>Карточка товара (неактивные и отсутствующие — 404).</summary>
    [HttpGet("products/{id}")]
    public ActionResult<ProductDto> Product(string id) => Ok(catalog.Product(id));

    private static decimal? ParsePrice(string? raw, string field, ValidationRules.Errors errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!decimal.TryParse(raw, out var value) || value < 0)
        {
            errors.Add(field, "Цена — число не меньше нуля");
            return null;
        }

        return value;
    }

    private static int? ParseInt(string? raw, string field, ValidationRules.Errors errors)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!int.TryParse(raw, out var value) || value < 1)
        {
            errors.Add(field, "Целое число больше нуля");
            return null;
        }

        return value;
    }
}
