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
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        var result = await auth.LoginAsync(request.Login, request.Password, ClientIp(HttpContext));
        AuthCookies.Write(HttpContext, result.AccessToken, result.AccessExpiresAt,
            result.RefreshToken, result.RefreshExpiresAt, SecureCookies);
        return Ok(new LoginResponseDto(result.User));
    }

    /// <summary>Выход: отзыв refresh-токена и очистка cookie.</summary>
    [HttpPost("logout")]
    [Authorize(Policy = "user")]
    public async Task<NoContentResult> Logout()
    {
        await auth.LogoutAsync(HttpContext.Request.Cookies[AuthCookies.Refresh]);
        AuthCookies.Clear(HttpContext);
        return NoContent();
    }

    /// <summary>Пользователь текущей сессии.</summary>
    [HttpGet("me")]
    [Authorize(Policy = "user")]
    public async Task<ActionResult<PublicUserDto>> Me() => Ok(await auth.MeAsync(UserId!));

    /// <summary>Обновление access-токена по refresh-cookie (ротация refresh).</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Refresh()
    {
        var result = await auth.RefreshAsync(HttpContext.Request.Cookies[AuthCookies.Refresh]);
        AuthCookies.Write(HttpContext, result.AccessToken, result.AccessExpiresAt,
            result.RefreshToken, result.RefreshExpiresAt, SecureCookies);
        return Ok(new LoginResponseDto(result.User));
    }

    /// <summary>Запрос кода восстановления: всегда 200 (оракула существования контакта нет).</summary>
    [HttpPost("recovery/request")]
    [AllowAnonymous]
    public async Task<ActionResult<RecoverySentDto>> RequestRecovery([FromBody] RecoveryRequestDto request) =>
        Ok(await auth.RequestRecoveryCodeAsync(request.Contact));

    /// <summary>Проверка кода восстановления → краткоживущий reset-токен.</summary>
    [HttpPost("recovery/confirm")]
    [AllowAnonymous]
    public async Task<ActionResult<ResetTokenDto>> ConfirmRecovery([FromBody] RecoveryConfirmDto request) =>
        Ok(await auth.ConfirmRecoveryCodeAsync(request.Contact, request.Code));

    /// <summary>Смена пароля по reset-токену; отзывает все refresh-токены пользователя.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
    {
        await auth.ResetPasswordAsync(request.ResetToken, request.Password);
        return Ok(new { ok = true });
    }
}

public sealed record LoginRequestDto(string? Login, string? Password);
