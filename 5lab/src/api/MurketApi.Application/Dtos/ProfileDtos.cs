namespace MurketApi.Application.Dtos;

public sealed record ProfileDto(string FullName, string Email, string? Phone);

public sealed record UpdateProfileDto(string FullName, string Email, string? Phone);

/// <summary>Черновик адреса доставки (создание и изменение).</summary>
public sealed record AddressDraftDto(
    string? Label,
    string Recipient,
    string Phone,
    string City,
    string Street,
    string House,
    string? Apartment,
    string? Entrance,
    string? Floor,
    string? Intercom,
    string? PostalCode,
    bool IsDefault);

public sealed record AddressDto(
    string Id,
    string UserId,
    string? Label,
    string Recipient,
    string Phone,
    string City,
    string Street,
    string House,
    string? Apartment,
    string? Entrance,
    string? Floor,
    string? Intercom,
    string? PostalCode,
    bool IsDefault);
