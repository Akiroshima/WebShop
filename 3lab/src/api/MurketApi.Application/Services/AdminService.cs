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
    public PagedResult<ProductDto> Products(string? search, PageRequest page) => unitOfWork.Execute(() =>
    {
        var query = new CatalogQuery
        {
            Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            ActiveOnly = false,
            Page = page,
        };
        var result = catalog.QueryProducts(query);
        var categoryNames = catalog.Categories().ToDictionary(category => category.Id, category => category.Name);
        var brandNames = catalog.Brands().ToDictionary(brand => brand.Id, brand => brand.Name);
        return new PagedResult<ProductDto>(
            [.. result.Items.Select(product => product.ToProductDto(categoryNames, brandNames))],
            result.Total, result.Page, result.PageSize);
    });

    public ProductDto CreateProduct(ProductDraftDto draft)
    {
        return unitOfWork.Execute(() =>
        {
            if (catalog.SkuInUse(NormalizeSku(draft.Sku)))
            {
                throw new ConflictException("Товар с таким артикулом уже существует");
            }

            if (!catalog.CategoryExists(draft.CategoryId))
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
            catalog.AddProduct(product);
            return ToDto(product);
        });
    }

    public ProductDto UpdateProduct(string productId, ProductDraftDto draft)
    {
        return unitOfWork.Execute(() =>
        {
            var product = catalog.FindProduct(productId) ?? throw new NotFoundException("Товар не найден");
            if (catalog.SkuInUse(NormalizeSku(draft.Sku), productId))
            {
                throw new ConflictException("Товар с таким артикулом уже существует");
            }

            if (!catalog.CategoryExists(draft.CategoryId))
            {
                throw new ValidationException("Данные заполнены неверно",
                    new Dictionary<string, string[]> { ["categoryId"] = ["Выберите категорию"] });
            }

            product.Update(draft.Sku, draft.Name, draft.Description, draft.CategoryId, draft.BrandId,
                draft.Price, draft.Stock, draft.ImageUrl, draft.IsActive);
            return ToDto(product);
        });
    }

    /// <summary>Удаление товара: позиции удаляются и из всех корзин (заказы хранят снимки и не меняются).</summary>
    public void DeleteProduct(string productId)
    {
        unitOfWork.Execute(() =>
        {
            var product = catalog.FindProduct(productId) ?? throw new NotFoundException("Товар не найден");
            catalog.RemoveProduct(product);
            foreach (var user in users.All())
            {
                carts.FindByUser(user.Id)?.RemoveItem(productId);
            }
        });
    }

    public IReadOnlyList<AdminUserDto> Users() =>
        [.. users.All().Select(user => user.ToAdminUser())];

    private ProductDto ToDto(Product product)
    {
        var categoryNames = catalog.Categories().ToDictionary(category => category.Id, category => category.Name);
        var brandNames = catalog.Brands().ToDictionary(brand => brand.Id, brand => brand.Name);
        return product.ToProductDto(categoryNames, brandNames);
    }

    private static string NormalizeSku(string? sku) => (sku ?? string.Empty).Trim().ToUpperInvariant();
}
