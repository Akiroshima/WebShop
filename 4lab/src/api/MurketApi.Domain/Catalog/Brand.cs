using MurketApi.Domain.Common;

namespace MurketApi.Domain.Catalog;

/// <summary>Бренд каталога.</summary>
public sealed class Brand
{
    public string Id { get; }

    public string Name { get; }

    public Brand(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Данные заполнены неверно",
                new Dictionary<string, string[]> { ["name"] = ["Название — от 1 до 100 символов"] });
        }

        Id = id;
        Name = name.Trim();
    }
}
