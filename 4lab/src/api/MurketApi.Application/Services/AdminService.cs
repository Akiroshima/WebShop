using MurketApi.Application.Abstractions;
using MurketApi.Application.Dtos;
using MurketApi.Application.Mapping;
using MurketApi.Domain.Cart;
using MurketApi.Domain.Catalog;
using MurketApi.Domain.Common;
using MurketApi.Domain.Users;

namespace MurketApi.Application.Services;

/// <summary>Модуль менеджера (роль администратора): товары и пользователи.</summary>
public sealed class AdminService(
    ICatalogRepository catalog,
    IUserRepository users,
    ICartRepository carts,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    /// <summary>Все товары (включая неактивные) с поиском по названию/артикулу — постранично.</summary>
    public Task<PagedResult<ProductDto>> ProductsAsync(string? search, PageRequest page) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var query = new CatalogQuery
            {
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                ActiveOnly = false,
                Page = page,
            };
            var result = await catalog.QueryProductsAsync(query);
            var categoryNames = (await catalog.CategoriesAsync())
                .ToDictionary(category => category.Id, category => category.Name);
            var brandNames = (await catalog.BrandsAsync())
                .ToDictionary(brand => brand.Id, brand => brand.Name);
            return new PagedResult<ProductDto>(
                [.. result.Items.Select(product => product.ToProductDto(categoryNames, brandNames))],
                result.Total, result.Page, result.PageSize);
        });

    public Task<ProductDto> CreateProductAsync(ProductDraftDto draft)
    {
        return unitOfWork.ExecuteAsync(async () =>
        {
            if (await catalog.SkuInUseAsync(NormalizeSku(draft.Sku)))
            {
                throw new ConflictException("Товар с таким артикулом уже существует");
            }

            if (!await catalog.CategoryExistsAsync(draft.CategoryId))
            {
                throw new ValidationException("Данные заполнены неверно",
                    new Dictionary<string, string[]> { ["categoryId"] = ["Выберите категорию"] });
            }

            var product = Product.Create(
                Guid.NewGuid().ToString("N"),
                draft.Sku,
                draft.Name,
                draft.Description,
                draft.CategoryId,
                draft.BrandId,
                draft.Price,
                draft.Stock,
                draft.ImageUrl,
                rating: 0,
                isPopular: false,
                isActive: draft.IsActive,
                createdAt: clock.UtcNow);
            await catalog.AddProductAsync(product);
            return await ToDtoAsync(product);
        });
    }

    public Task<ProductDto> UpdateProductAsync(string productId, ProductDraftDto draft)
    {
        return unitOfWork.ExecuteAsync(async () =>
        {
            var product = await catalog.FindProductAsync(productId)
                ?? throw new NotFoundException("Товар не найден");
            if (await catalog.SkuInUseAsync(NormalizeSku(draft.Sku), productId))
            {
                throw new ConflictException("Товар с таким артикулом уже существует");
            }

            if (!await catalog.CategoryExistsAsync(draft.CategoryId))
            {
                throw new ValidationException("Данные заполнены неверно",
                    new Dictionary<string, string[]> { ["categoryId"] = ["Выберите категорию"] });
            }

            product.Update(draft.Sku, draft.Name, draft.Description, draft.CategoryId, draft.BrandId,
                draft.Price, draft.Stock, draft.ImageUrl, draft.IsActive);
            return await ToDtoAsync(product);
        });
    }

    /// <summary>Удаление товара: позиции удаляются и из всех корзин (заказы хранят снимки и не меняются).</summary>
    public Task DeleteProductAsync(string productId)
    {
        return unitOfWork.ExecuteAsync(async () =>
        {
            var product = await catalog.FindProductAsync(productId)
                ?? throw new NotFoundException("Товар не найден");
            await catalog.RemoveProductAsync(product);
            foreach (var user in await users.AllAsync())
            {
                var cart = await carts.FindByUserAsync(user.Id);
                if (cart?.FindItem(productId) is not null)
                {
                    cart.RemoveItem(productId);
                    await carts.SaveAsync(cart);
                }
            }
        });
    }

    public async Task<IReadOnlyList<AdminUserDto>> UsersAsync() =>
        [.. (await users.AllAsync()).Select(user => user.ToAdminUser())];

    private async Task<ProductDto> ToDtoAsync(Product product)
    {
        var categoryNames = (await catalog.CategoriesAsync())
            .ToDictionary(category => category.Id, category => category.Name);
        var brandNames = (await catalog.BrandsAsync())
            .ToDictionary(brand => brand.Id, brand => brand.Name);
        return product.ToProductDto(categoryNames, brandNames);
    }

    private static string NormalizeSku(string? sku) => (sku ?? string.Empty).Trim().ToUpperInvariant();
}
