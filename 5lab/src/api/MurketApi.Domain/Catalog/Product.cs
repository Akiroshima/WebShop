using MurketApi.Domain.Common;

namespace MurketApi.Domain.Catalog;

/// <summary>Товар каталога. Артикул хранится в верхнем регистре и уникален.</summary>
public sealed class Product
{
    public string Id { get; private set; } = string.Empty;

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string CategoryId { get; private set; } = string.Empty;

    public string? BrandId { get; private set; }

    public decimal Price { get; private set; }

    public int Stock { get; private set; }

    public string ImageUrl { get; private set; } = string.Empty;

    /// <summary>Рейтинг товара (демо-атрибут каталога).</summary>
    public double Rating { get; private set; }

    public bool IsPopular { get; private set; }

    /// <summary>Неактивные товары скрыты из каталога покупателей.</summary>
    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private Product()
    {
    }

    public static Product Create(string id, string sku, string name, string description, string categoryId,
        string? brandId, decimal price, int stock, string imageUrl, double rating, bool isPopular,
        bool isActive, DateTimeOffset createdAt)
    {
        var errors = new Dictionary<string, string[]>();
        sku = (sku ?? string.Empty).Trim().ToUpperInvariant();
        if (sku.Length < 2)
        {
            errors["sku"] = ["Укажите артикул (минимум 2 символа)"];
        }

        ValidateNameAndMoney(name, price, stock, categoryId, errors);
        if (errors.Count > 0)
        {
            throw new ValidationException("Данные заполнены неверно", errors);
        }

        return new Product
        {
            Id = id,
            Sku = sku,
            Name = name.Trim(),
            Description = description ?? string.Empty,
            CategoryId = categoryId,
            BrandId = string.IsNullOrWhiteSpace(brandId) ? null : brandId,
            Price = price,
            Stock = stock,
            ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? "assets/img/products/korm.svg" : imageUrl,
            Rating = rating,
            IsPopular = isPopular,
            IsActive = isActive,
            CreatedAt = createdAt,
        };
    }

    /// <summary>Изменение товара менеджером (дубликат артикула проверяет прикладной слой).</summary>
    public void Update(string sku, string name, string description, string categoryId, string? brandId,
        decimal price, int stock, string imageUrl, bool isActive)
    {
        var errors = new Dictionary<string, string[]>();
        sku = (sku ?? string.Empty).Trim().ToUpperInvariant();
        if (sku.Length < 2)
        {
            errors["sku"] = ["Укажите артикул (минимум 2 символа)"];
        }

        ValidateNameAndMoney(name, price, stock, categoryId, errors);
        if (errors.Count > 0)
        {
            throw new ValidationException("Данные заполнены неверно", errors);
        }

        Sku = sku;
        Name = name.Trim();
        Description = description ?? string.Empty;
        CategoryId = categoryId;
        BrandId = string.IsNullOrWhiteSpace(brandId) ? null : brandId;
        Price = price;
        Stock = stock;
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            ImageUrl = imageUrl;
        }

        IsActive = isActive;
    }

    /// <summary>Списание остатка при оформлении заказа; не хватает — конфликт.</summary>
    public void Reserve(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ConflictException("Некорректное количество товара");
        }

        if (quantity > Stock)
        {
            throw new ConflictException($"В наличии только {Stock} шт. товара «{Name}»");
        }

        Stock -= quantity;
    }

    /// <summary>Возврат остатка при отмене заказа.</summary>
    public void Return(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ConflictException("Некорректное количество товара");
        }

        Stock += quantity;
    }

    private static void ValidateNameAndMoney(string? name, decimal price, int stock, string? categoryId,
        Dictionary<string, string[]> errors)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length is < 1 or > 300)
        {
            errors["name"] = ["Название — от 1 до 300 символов"];
        }

        if (price < 0)
        {
            errors["price"] = ["Цена не может быть отрицательной"];
        }

        if (stock < 0)
        {
            errors["stock"] = ["Остаток — целое число не меньше нуля"];
        }

        if (string.IsNullOrWhiteSpace(categoryId))
        {
            errors["categoryId"] = ["Выберите категорию"];
        }
    }
}
