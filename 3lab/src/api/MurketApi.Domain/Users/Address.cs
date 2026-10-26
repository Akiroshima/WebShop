using PhoneUtil = MurketApi.Domain.Common.Phone;
using MurketApi.Domain.Common;

namespace MurketApi.Domain.Users;

/// <summary>Сохранённый адрес доставки личного кабинета (не более MaxPerUser у одного пользователя).</summary>
public sealed class Address
{
    public const int MaxPerUser = 10;

    public string Id { get; private set; } = string.Empty;

    public string UserId { get; private set; } = string.Empty;

    public string? Label { get; private set; }

    public string Recipient { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string Street { get; private set; } = string.Empty;

    public string House { get; private set; } = string.Empty;

    public string? Apartment { get; private set; }

    public string? Entrance { get; private set; }

    public string? Floor { get; private set; }

    public string? Intercom { get; private set; }

    public string? PostalCode { get; private set; }

    public bool IsDefault { get; private set; }

    private Address()
    {
    }

    public static Address Create(string id, string userId, string? label, string recipient, string phone,
        string city, string street, string house, string? apartment, string? entrance, string? floor,
        string? intercom, string? postalCode, bool isDefault)
    {
        Validate(recipient, phone, city, street, house);
        return new Address
        {
            Id = id,
            UserId = userId,
            Label = NormalizeOptional(label),
            Recipient = recipient.Trim(),
            Phone = PhoneUtil.Normalize(phone),
            City = city.Trim(),
            Street = street.Trim(),
            House = house.Trim(),
            Apartment = NormalizeOptional(apartment),
            Entrance = NormalizeOptional(entrance),
            Floor = NormalizeOptional(floor),
            Intercom = NormalizeOptional(intercom),
            PostalCode = NormalizeOptional(postalCode),
            IsDefault = isDefault,
        };
    }

    /// <summary>Обновляет поля; переключение «по умолчанию» у других адресов orchestrated прикладным слоем.</summary>
    public void Update(string? label, string recipient, string phone, string city, string street, string house,
        string? apartment, string? entrance, string? floor, string? intercom, string? postalCode, bool isDefault)
    {
        Validate(recipient, phone, city, street, house);
        Label = NormalizeOptional(label);
        Recipient = recipient.Trim();
        Phone = PhoneUtil.Normalize(phone);
        City = city.Trim();
        Street = street.Trim();
        House = house.Trim();
        Apartment = NormalizeOptional(apartment);
        Entrance = NormalizeOptional(entrance);
        Floor = NormalizeOptional(floor);
        Intercom = NormalizeOptional(intercom);
        PostalCode = NormalizeOptional(postalCode);
        IsDefault = isDefault;
    }

    private static void Validate(string recipient, string phone, string city, string street, string house)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(recipient))
        {
            errors["recipient"] = ["Укажите получателя"];
        }

        if (!PhoneUtil.IsValidE164(PhoneUtil.Normalize(phone)))
        {
            errors["phone"] = ["Введите корректный номер телефона"];
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            errors["city"] = ["Укажите город"];
        }

        if (string.IsNullOrWhiteSpace(street))
        {
            errors["street"] = ["Укажите улицу"];
        }

        if (string.IsNullOrWhiteSpace(house))
        {
            errors["house"] = ["Укажите дом"];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Данные заполнены неверно", errors);
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
