namespace MurketApi.Domain.Users;

/// <summary>Пользователи. Реализация — EF Core (PostgreSQL); все операции асинхронны.</summary>
public interface IUserRepository
{
    Task<User?> FindByIdAsync(string id);

    /// <summary>Поиск для входа: совпадение по логину, email (без регистра) или телефону (E.164).</summary>
    Task<User?> FindForLoginAsync(string login);

    Task<bool> EmailInUseAsync(string email, string? exceptUserId = null);

    Task<bool> PhoneInUseAsync(string phone, string? exceptUserId = null);

    Task AddAsync(User user);

    Task<IReadOnlyList<User>> AllAsync();
}
