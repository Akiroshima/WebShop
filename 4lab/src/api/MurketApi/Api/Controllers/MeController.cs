using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MurketApi.Api.Infrastructure;
using MurketApi.Application.Dtos;
using MurketApi.Application.Services;

namespace MurketApi.Api.Controllers;

/// <summary>Личный кабинет: профиль, смена пароля, адреса доставки.</summary>
[ApiController]
[Route("api/v1/me")]
[Authorize(Policy = "user")]
public sealed class MeController(ProfileService profile) : ControllerBase
{
    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

    /// <summary>Данные аккаунта.</summary>
    [HttpGet("profile")]
    public async Task<ActionResult<ProfileDto>> GetProfile() => Ok(await profile.GetProfileAsync(UserId));

    /// <summary>Изменение ФИО, email, телефона.</summary>
    [HttpPut("profile")]
    public async Task<ActionResult<ProfileDto>> UpdateProfile([FromBody] UpdateProfileDto request) =>
        Ok(await profile.UpdateProfileAsync(UserId, request));

    /// <summary>Смена пароля: текущий обязателен; прочие refresh-токены отзываются.</summary>
    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        await profile.ChangePasswordAsync(UserId, request, HttpContext.Request.Cookies[AuthCookies.Refresh]);
        return Ok(new { ok = true });
    }

    /// <summary>Список адресов доставки.</summary>
    [HttpGet("addresses")]
    public async Task<ActionResult<IReadOnlyList<AddressDto>>> Addresses() =>
        Ok(await profile.AddressesAsync(UserId));

    /// <summary>Создать адрес → 201.</summary>
    [HttpPost("addresses")]
    public async Task<ActionResult<AddressDto>> CreateAddress([FromBody] AddressDraftDto draft) =>
        StatusCode(StatusCodes.Status201Created, await profile.CreateAddressAsync(UserId, draft));

    /// <summary>Изменить адрес.</summary>
    [HttpPut("addresses/{id}")]
    public async Task<ActionResult<AddressDto>> UpdateAddress(string id, [FromBody] AddressDraftDto draft) =>
        Ok(await profile.UpdateAddressAsync(UserId, id, draft));

    /// <summary>Удалить адрес.</summary>
    [HttpDelete("addresses/{id}")]
    public async Task<NoContentResult> DeleteAddress(string id)
    {
        await profile.DeleteAddressAsync(UserId, id);
        return NoContent();
    }
}
