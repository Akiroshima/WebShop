namespace MurketApi.Application.Dtos;

/// <summary>Публичное представление пользователя (ответ /auth/login, /auth/me, /auth/refresh).</summary>
public sealed record PublicUserDto(
    string Id,
    string Login,
    string FullName,
    string Email,
    string? Phone,
    string Role);

/// <summary>Ответ входа: {user} — форма контракта клиента.</summary>
public sealed record LoginResponseDto(PublicUserDto User);

public sealed record RecoveryRequestDto(string Contact);

public sealed record RecoveryConfirmDto(string Contact, string Code);

public sealed record ResetPasswordDto(string ResetToken, string Password);

public sealed record ChangePasswordDto(string CurrentPassword, string Password);

/// <summary>Ответ запроса кода: канал и маскированный контакт (оракула существования нет).</summary>
public sealed record RecoverySentDto(string Channel, string MaskedContact);

public sealed record ResetTokenDto(string ResetToken);
