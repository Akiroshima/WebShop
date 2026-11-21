namespace MurketApi.Domain.Users;

/// <summary>Адреса доставки. Заглушка БД: InMemoryAddressRepository.</summary>
public interface IAddressRepository
{
    IReadOnlyList<Address> ForUser(string userId);

    /// <summary>Ищет адрес только среди адресов пользователя (чужие → null, существование не раскрывается).</summary>
    Address? FindForUser(string userId, string addressId);

    int CountForUser(string userId);

    void Add(Address address);

    void Remove(Address address);
}
