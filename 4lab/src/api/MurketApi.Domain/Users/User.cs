using PhoneUtil = MurketApi.Domain.Common.Phone;
using MurketApi.Domain.Common;

namespace MurketApi.Domain.Users;

/// <summary>Учётная запись. Логин, email и телефон — уникальны; телефон хранится в E.164 (+7…).</summary>
public sealed class User
{
    public string Id { get; private set; } = string.Empty;

    /// <summary>Логин (уникален, сравнение без учёта регистра).</summary>
    public string Login { get; private set; } = string.Empty;

    /// <summary>Email (нормализован к нижнему регистру, уникален).</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Телефон в E.164 или null — не указан.</summary>
    public string? Phone { get; private set; }

    /// <summary>PBKDF2-хэш пароля (соль и параметры — внутри строки).</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private User()
    {
    }

    public static User Create(string id, string login, string email, string? phone, string passwordHash,
        string fullName, UserRole role, DateTimeOffset createdAt)
    {
        Validate(login, email, phone, fullName);
        return new User
        {
            Id = id,
            Login = login.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            Phone = string.IsNullOrEmpty(phone) ? null : phone,
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            Role = role,
            CreatedAt = createdAt,
        };
    }

    /// <summary>Изменяет профиль; проверку форматов и уникальности выполняет прикладной слой.</summary>
    public void ChangeProfile(string fullName, string email, string? phone)
    {
        Validate(Login, email, phone, fullName);
        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        Phone = string.IsNullOrEmpty(phone) ? null : phone;
    }

    public void ChangePassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ValidationException("Данные заполнены неверно",
                new Dictionary<string, string[]> { ["password"] = ["Пароль обязателен"] });
        }

        PasswordHash = passwordHash;
    }

    private static void Validate(string login, string email, string? phone, string fullName)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(login))
        {
            errors["login"] = ["Укажите логин"];
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            errors["email"] = ["Введите email"];
        }

        if (phone is not null && !PhoneUtil.IsValidE164(phone))
        {
            errors["phone"] = ["Введите корректный номер телефона"];
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            errors["fullName"] = ["Укажите имя и фамилию"];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Данные заполнены неверно", errors);
        }
    }
}
