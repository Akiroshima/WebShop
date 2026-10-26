using MurketApi.Domain.Common;

namespace MurketApi.Domain.Users;

/// <summary>Пользователи. Заглушка БД: InMemoryUserRepository; контракт не изменится при переходе на EF Core.</summary>
public interface IUserRepository
{
    User? FindById(string id);

    /// <summary>Поиск для входа: совпадение по логину, email (без регистра) или телефону (E.164).</summary>
    User? FindForLogin(string login);

    bool EmailInUse(string email, string? exceptUserId = null);

    bool PhoneInUse(string phone, string? exceptUserId = null);

    void Add(User user);

    IReadOnlyList<User> All();
}
