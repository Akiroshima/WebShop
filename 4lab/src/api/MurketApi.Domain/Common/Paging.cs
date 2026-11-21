namespace MurketApi.Domain.Common;

/// <summary>Запрос страницы большого набора данных. Страницы нумеруются с 1.</summary>
public readonly record struct PageRequest(int Page, int PageSize)
{
    public static readonly PageRequest Default = new(1, 12);

    /// <summary>Нормализует страницу и размер (страница ≥ 1, размер 1..48).</summary>
    public static PageRequest Normalize(int? page, int? pageSize, int defaultPageSize = 12, int maxPageSize = 48)
    {
        var p = page is > 0 ? page.Value : 1;
        var size = pageSize is > 0 ? pageSize.Value : defaultPageSize;
        if (size > maxPageSize)
        {
            size = maxPageSize;
        }

        return new PageRequest(p, size);
    }

    public int Skip => (Page - 1) * PageSize;
}

/// <summary>Страница большого набора данных: {items, total, page, pageSize}.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public static PagedResult<T> Empty(PageRequest request) => new([], 0, request.Page, request.PageSize);
}
