namespace MurketApi.Application.Dtos;

/// <summary>Черновик товара (создание и изменение менеджером).</summary>
public sealed record ProductDraftDto(
    string Sku,
    string Name,
    string Description,
    string CategoryId,
    string? BrandId,
    decimal Price,
    int Stock,
    string ImageUrl,
    bool IsActive);

public sealed record AdminUserDto(
    string Id,
    string Login,
    string FullName,
    string Email,
    string? Phone,
    string Role,
    DateTimeOffset CreatedAt);
