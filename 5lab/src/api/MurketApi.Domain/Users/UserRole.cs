namespace MurketApi.Domain.Users;

/// <summary>Роль учётной записи. Регистрации в системе нет: покупателя и администратора создаёт сид.</summary>
public enum UserRole
{
    Customer = 0,
    Admin = 1,
}

public static class UserRoleExtensions
{
    public static string ToApiString(this UserRole role) => role switch
    {
        UserRole.Customer => "customer",
        UserRole.Admin => "admin",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };
}
