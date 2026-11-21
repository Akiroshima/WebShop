using MurketApi.Domain.Common;

namespace MurketApi.Domain.Catalog;

/// <summary>Сортировка каталога (значения совпадают с query-параметром sort клиента).</summary>
public enum CatalogSort
{
    Popular = 0,
    Newest,
    PriceAsc,
    PriceDesc,
    NameAsc,
}

/// <summary>Параметры выборки товаров каталога: фильтры, сортировка, страница.</summary>
public sealed record CatalogQuery
{
    /// <summary>Поиск по названию, артикулу и бренду (без учета регистра).</summary>
    public string? Search { get; init; }

    public string? CategoryId { get; init; }

    public IReadOnlyList<string> BrandIds { get; init; } = [];

    public decimal? PriceMin { get; init; }

    public decimal? PriceMax { get; init; }

    /// <summary>Только товары с остатком.</summary>
    public bool OnlyAvailable { get; init; }

    public CatalogSort Sort { get; init; } = CatalogSort.Popular;

    /// <summary>Только активные товары (витрина); false — весь список для менеджера.</summary>
    public bool ActiveOnly { get; init; } = true;

    public PageRequest Page { get; init; } = PageRequest.Default;
}
