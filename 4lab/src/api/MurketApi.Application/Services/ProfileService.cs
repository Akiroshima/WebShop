using MurketApi.Application.Abstractions;
using MurketApi.Application.Dtos;
using MurketApi.Application.Mapping;
using MurketApi.Application.Validation;
using MurketApi.Domain.Common;
using MurketApi.Domain.Security;
using MurketApi.Domain.Users;

namespace MurketApi.Application.Services;

/// <summary>Личный кабинет: профиль, смена пароля, адреса доставки.</summary>
public sealed class ProfileService(
    IUserRepository users,
    IAddressRepository addresses,
    ISecurityTokenRepository tokens,
    IPasswordHasher hasher,
    ITokenHasher tokenHasher,
    IKdfCounter kdf,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    public async Task<ProfileDto> GetProfileAsync(string userId)
    {
        var user = await RequireUserAsync(userId);
        return new ProfileDto(user.FullName, user.Email, user.Phone);
    }

    /// <summary>Изменение ФИО, email, телефона; занятые контакты — конфликт.</summary>
    public Task<ProfileDto> UpdateProfileAsync(string userId, UpdateProfileDto request)
    {
        var errors = new ValidationRules.Errors();
        var fullName = (request.FullName ?? string.Empty).Trim();
        var email = ValidationRules.NormalizeEmail(request.Email);
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : Phone.Normalize(request.Phone);

        if (fullName.Length == 0)
        {
            errors.Add("fullName", "Укажите имя и фамилию");
        }

        if (!ValidationRules.IsValidEmail(email))
        {
            errors.Add("email", "Введите корректный email");
        }

        if (phone is not null && !Phone.IsValidE164(phone))
        {
            errors.Add("phone", "Введите корректный номер телефона");
        }

        if (errors.Any)
        {
            throw errors.ToException();
        }

        return unitOfWork.ExecuteAsync(async () =>
        {
            var user = await RequireUserAsync(userId);
            if (await users.EmailInUseAsync(email, userId))
            {
                throw new ConflictException("Пользователь с таким email уже существует");
            }

            if (phone is not null && await users.PhoneInUseAsync(phone, userId))
            {
                throw new ConflictException("Пользователь с таким телефоном уже существует");
            }

            user.ChangeProfile(fullName, email, phone);
            return new ProfileDto(user.FullName, user.Email, user.Phone);
        });
    }

    /// <summary>Смена пароля: текущий обязателен; после смены прочие refresh-токены отзываются.</summary>
    public Task ChangePasswordAsync(string userId, ChangePasswordDto request, string? currentRefreshRaw)
    {
        var errors = new ValidationRules.Errors();
        if (string.IsNullOrEmpty(request.CurrentPassword))
        {
            errors.Add("currentPassword", "Укажите текущий пароль");
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            errors.Add("password", "Укажите новый пароль");
        }

        if (errors.Any)
        {
            throw errors.ToException();
        }

        return unitOfWork.ExecuteAsync(async () =>
        {
            var user = await RequireUserAsync(userId);
            kdf.Increment("change_password");
            if (!hasher.Verify(user.PasswordHash, request.CurrentPassword, "change_password"))
            {
                throw new ValidationException("Неверный текущий пароль");
            }

            ValidationRules.ValidatePassword(errors, request.Password);
            if (errors.Any)
            {
                throw errors.ToException();
            }

            user.ChangePassword(hasher.Hash(request.Password, "change_password"));

            // Отзываем все refresh-токены, кроме токена текущей сессии.
            var currentHash = currentRefreshRaw is null ? null : tokenHasher.Hash(currentRefreshRaw);
            var current = currentHash is null ? null : await tokens.FindRefreshTokenAsync(currentHash);
            foreach (var token in await tokens.RefreshTokensOfAsync(user.Id))
            {
                if (current is null || token.Hash != current.Hash)
                {
                    await tokens.RevokeRefreshTokenAsync(token, clock.UtcNow);
                }
            }
        });
    }

    // ----- Адреса -----

    public async Task<IReadOnlyList<AddressDto>> AddressesAsync(string userId) =>
        [.. (await addresses.ForUserAsync(userId)).Select(address => address.ToAddressDto())];

    public Task<AddressDto> CreateAddressAsync(string userId, AddressDraftDto draft)
    {
        return unitOfWork.ExecuteAsync(async () =>
        {
            if (await addresses.CountForUserAsync(userId) >= Address.MaxPerUser)
            {
                throw new ConflictException($"Нельзя сохранить больше {Address.MaxPerUser} адресов");
            }

            var address = Address.Create(
                Guid.NewGuid().ToString("N"), userId, draft.Label, draft.Recipient, draft.Phone,
                draft.City, draft.Street, draft.House, draft.Apartment, draft.Entrance, draft.Floor,
                draft.Intercom, draft.PostalCode, draft.IsDefault);
            await UnsetOtherDefaultsAsync(userId, address.Id, address.IsDefault);
            await addresses.AddAsync(address);
            return address.ToAddressDto();
        });
    }

    public Task<AddressDto> UpdateAddressAsync(string userId, string addressId, AddressDraftDto draft)
    {
        return unitOfWork.ExecuteAsync(async () =>
        {
            var address = await addresses.FindForUserAsync(userId, addressId)
                ?? throw new NotFoundException("Адрес не найден");
            address.Update(draft.Label, draft.Recipient, draft.Phone, draft.City, draft.Street,
                draft.House, draft.Apartment, draft.Entrance, draft.Floor, draft.Intercom,
                draft.PostalCode, draft.IsDefault);
            await UnsetOtherDefaultsAsync(userId, address.Id, address.IsDefault);
            return address.ToAddressDto();
        });
    }

    public Task DeleteAddressAsync(string userId, string addressId) =>
        unitOfWork.ExecuteAsync(async () =>
        {
            var address = await addresses.FindForUserAsync(userId, addressId)
                ?? throw new NotFoundException("Адрес не найден");
            await addresses.RemoveAsync(address);
        });

    /// <summary>Адрес «по умолчанию» у пользователя может быть только один.</summary>
    private async Task UnsetOtherDefaultsAsync(string userId, string addressId, bool isDefault)
    {
        if (!isDefault)
        {
            return;
        }

        foreach (var other in await addresses.ForUserAsync(userId))
        {
            if (other.IsDefault && other.Id != addressId)
            {
                other.Update(other.Label, other.Recipient, other.Phone, other.City, other.Street, other.House,
                    other.Apartment, other.Entrance, other.Floor, other.Intercom, other.PostalCode, false);
            }
        }
    }

    private async Task<User> RequireUserAsync(string userId) =>
        await users.FindByIdAsync(userId) ?? throw new UnauthorizedException("Сессия недействительна. Войдите снова");
}
