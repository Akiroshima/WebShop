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
    public ActionResult<ProfileDto> GetProfile() => Ok(profile.GetProfile(UserId));

    /// <summary>Изменить ФИО, email, телефон.</summary>
    [HttpPut("profile")]
    public ActionResult<ProfileDto> UpdateProfile([FromBody] UpdateProfileDto request) =>
        Ok(profile.UpdateProfile(UserId, request));

    /// <summary>Смена пароля: текущий обязателен; прочие refresh-токены отзываются.</summary>
    [HttpPut("password")]
    public IActionResult ChangePassword([FromBody] ChangePasswordDto request)
    {
        profile.ChangePassword(UserId, request, HttpContext.Request.Cookies[AuthCookies.Refresh]);
        return Ok(new { ok = true });
    }

    /// <summary>Список адресов доставки.</summary>
    [HttpGet("addresses")]
    public ActionResult<IReadOnlyList<AddressDto>> Addresses() => Ok(profile.Addresses(UserId));

    /// <summary>Создать адрес → 201.</summary>
    [HttpPost("addresses")]
    public ActionResult<AddressDto> CreateAddress([FromBody] AddressDraftDto draft) =>
        StatusCode(StatusCodes.Status201Created, profile.CreateAddress(UserId, draft));

    /// <summary>Изменить адрес.</summary>
    [HttpPut("addresses/{id}")]
    public ActionResult<AddressDto> UpdateAddress(string id, [FromBody] AddressDraftDto draft) =>
        Ok(profile.UpdateAddress(UserId, id, draft));

    /// <summary>Удалить адрес.</summary>
    [HttpDelete("addresses/{id}")]
    public NoContentResult DeleteAddress(string id)
    {
        profile.DeleteAddress(UserId, id);
        return NoContent();
    }
}
