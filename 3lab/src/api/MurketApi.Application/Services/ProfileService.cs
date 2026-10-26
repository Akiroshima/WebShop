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
    public ProfileDto GetProfile(string userId)
    {
        var user = RequireUser(userId);
        return new ProfileDto(user.FullName, user.Email, user.Phone);
    }

    /// <summary>Изменение ФИО, email, телефона; занятые контакты — конфликт.</summary>
    public ProfileDto UpdateProfile(string userId, UpdateProfileDto request)
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

        return unitOfWork.Execute(() =>
        {
            var user = RequireUser(userId);
            if (users.EmailInUse(email, userId))
            {
                throw new ConflictException("Пользователь с таким email уже существует");
            }

            if (phone is not null && users.PhoneInUse(phone, userId))
            {
                throw new ConflictException("Пользователь с таким телефоном уже существует");
            }

            user.ChangeProfile(fullName, email, phone);
            return new ProfileDto(user.FullName, user.Email, user.Phone);
        });
    }

    /// <summary>Смена пароля: текущий обязателен; после смены прочие refresh-токены отзываются.</summary>
    public void ChangePassword(string userId, ChangePasswordDto request, string? currentRefreshRaw)
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

        unitOfWork.Execute(() =>
        {
            var user = RequireUser(userId);
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
            var current = currentHash is null ? null : tokens.FindRefreshToken(currentHash);
            foreach (var token in tokens.RefreshTokensOf(user.Id))
            {
                if (current is null || token.Hash != current.Hash)
                {
                    tokens.RevokeRefreshToken(token, clock.UtcNow);
                }
            }
        });
    }

    // ----- Адреса -----

    public IReadOnlyList<AddressDto> Addresses(string userId) =>
        [.. addresses.ForUser(userId).Select(address => address.ToAddressDto())];

    public AddressDto CreateAddress(string userId, AddressDraftDto draft)
    {
        return unitOfWork.Execute(() =>
        {
            if (addresses.CountForUser(userId) >= Address.MaxPerUser)
            {
                throw new ConflictException($"Нельзя сохранить больше {Address.MaxPerUser} адресов");
            }

            var address = Address.Create(
                Guid.NewGuid().ToString("N"), userId, draft.Label, draft.Recipient, draft.Phone,
                draft.City, draft.Street, draft.House, draft.Apartment, draft.Entrance, draft.Floor,
                draft.Intercom, draft.PostalCode, draft.IsDefault);
            UnsetOtherDefaults(userId, address.Id, address.IsDefault);
            addresses.Add(address);
            return address.ToAddressDto();
        });
    }

    public AddressDto UpdateAddress(string userId, string addressId, AddressDraftDto draft)
    {
        return unitOfWork.Execute(() =>
        {
            var address = addresses.FindForUser(userId, addressId)
                ?? throw new NotFoundException("Адрес не найден");
            address.Update(draft.Label, draft.Recipient, draft.Phone, draft.City, draft.Street,
                draft.House, draft.Apartment, draft.Entrance, draft.Floor, draft.Intercom,
                draft.PostalCode, draft.IsDefault);
            UnsetOtherDefaults(userId, address.Id, address.IsDefault);
            return address.ToAddressDto();
        });
    }

    public void DeleteAddress(string userId, string addressId)
    {
        unitOfWork.Execute(() =>
        {
            var address = addresses.FindForUser(userId, addressId)
                ?? throw new NotFoundException("Адрес не найден");
            addresses.Remove(address);
        });
    }

    /// <summary>Адрес «по умолчанию» у пользователя может быть только один.</summary>
    private void UnsetOtherDefaults(string userId, string addressId, bool isDefault)
    {
        if (!isDefault)
        {
            return;
        }

        foreach (var other in addresses.ForUser(userId).Where(other => other.IsDefault && other.Id != addressId))
        {
            other.Update(other.Label, other.Recipient, other.Phone, other.City, other.Street, other.House,
                other.Apartment, other.Entrance, other.Floor, other.Intercom, other.PostalCode, false);
        }
    }

    private User RequireUser(string userId) =>
        users.FindById(userId) ?? throw new UnauthorizedException("Сессия недействительна. Войдите снова");
}
