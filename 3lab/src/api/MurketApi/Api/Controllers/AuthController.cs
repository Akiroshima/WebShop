using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using MurketApi.Api.Infrastructure;
using MurketApi.Application.Dtos;
using MurketApi.Application.Services;

namespace MurketApi.Api.Controllers;

/// <summary>Вход, выход, обновление сессии и восстановление доступа.</summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(AuthService auth, IWebHostEnvironment environment) : ControllerBase
{
    private bool SecureCookies => !environment.IsDevelopment();

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    private static string ClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    /// <summary>Вход по логину, email или телефону; ставит httpOnly-cookie access + refresh.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public ActionResult<LoginResponseDto> Login([FromBody] LoginRequestDto request)
    {
        var result = auth.Login(request.Login, request.Password, ClientIp(HttpContext));
        AuthCookies.Write(HttpContext, result.AccessToken, result.AccessExpiresAt,
            result.RefreshToken, result.RefreshExpiresAt, SecureCookies);
        return Ok(new LoginResponseDto(result.User));
    }

    /// <summary>Выход: отзыв refresh-токена и очистка cookie.</summary>
    [HttpPost("logout")]
    [Authorize(Policy = "user")]
    public NoContentResult Logout()
    {
        auth.Logout(HttpContext.Request.Cookies[AuthCookies.Refresh]);
        AuthCookies.Clear(HttpContext);
        return NoContent();
    }

    /// <summary>Пользователь текущей сессии.</summary>
    [HttpGet("me")]
    [Authorize(Policy = "user")]
    public ActionResult<PublicUserDto> Me() => Ok(auth.Me(UserId!));

    /// <summary>Обновление access-токена по refresh-cookie (ротация refresh).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public ActionResult<LoginResponseDto> Refresh()
    {
        var result = auth.Refresh(HttpContext.Request.Cookies[AuthCookies.Refresh]);
        AuthCookies.Write(HttpContext, result.AccessToken, result.AccessExpiresAt,
            result.RefreshToken, result.RefreshExpiresAt, SecureCookies);
        return Ok(new LoginResponseDto(result.User));
    }

    /// <summary>Запрос кода восстановления: всегда 200 (оракула существования контакта нет).</summary>
    [HttpPost("recovery/request")]
    [AllowAnonymous]
    public ActionResult<RecoverySentDto> RequestRecovery([FromBody] RecoveryRequestDto request) =>
        Ok(auth.RequestRecoveryCode(request.Contact));

    /// <summary>Проверка кода восстановления → краткоживущий reset-токен.</summary>
    [HttpPost("recovery/confirm")]
    [AllowAnonymous]
    public ActionResult<ResetTokenDto> ConfirmRecovery([FromBody] RecoveryConfirmDto request) =>
        Ok(auth.ConfirmRecoveryCode(request.Contact, request.Code));

    /// <summary>Смена пароля по reset-токену; отзывает все refresh-токены пользователя.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public IActionResult ResetPassword([FromBody] ResetPasswordDto request)
    {
        auth.ResetPassword(request.ResetToken, request.Password);
        return Ok(new { ok = true });
    }
}

public sealed record LoginRequestDto(string? Login, string? Password);
