namespace MurketApi.Domain.Users;

/// <summary>Адреса доставки. Реализация — EF Core (PostgreSQL).</summary>
public interface IAddressRepository
{
    Task<IReadOnlyList<Address>> ForUserAsync(string userId);

    /// <summary>Ищет адрес только среди адресов пользователя (чужие → null, существование не раскрывается).</summary>
    Task<Address?> FindForUserAsync(string userId, string addressId);

    Task<int> CountForUserAsync(string userId);

    Task AddAsync(Address address);

    Task RemoveAsync(Address address);
}
