namespace MurketApi.Application.Dtos;

public sealed record CategoryDto(string Id, string Name, int Count);

public sealed record BrandDto(string Id, string Name, int Count);

/// <summary>Товар каталога: плоское DTO с именами категории и бренда (контракт клиента).</summary>
public sealed record ProductDto(
    string Id,
    string Sku,
    string Name,
    string Description,
    string CategoryId,
    string Category,
    string? BrandId,
    string? Brand,
    decimal Price,
    int Stock,
    string ImageUrl,
    double Rating,
    bool IsPopular,
    bool IsActive,
    DateTimeOffset CreatedAt);
