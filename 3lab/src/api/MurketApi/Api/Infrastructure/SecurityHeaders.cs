namespace MurketApi.Api.Infrastructure;

/// <summary>Защитные заголовки для всех ответов (статика и SPA: nosniff, DENY).</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            return Task.CompletedTask;
        });
        await next(context);
    }
}

/// <summary>Имена и параметры httpOnly-cookie аутентификации.</summary>
public static class AuthCookies
{
    public const string Access = "murket_at";

    public const string Refresh = "murket_rt";

    public static void Write(HttpContext context, string accessToken, DateTimeOffset accessExpires,
        string refreshToken, DateTimeOffset refreshExpires, bool secure)
    {
        context.Response.Cookies.Append(Access, accessToken, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = secure,
            Expires = accessExpires,
            Path = "/",
        });
        context.Response.Cookies.Append(Refresh, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = secure,
            Expires = refreshExpires,
            Path = "/",
        });
    }

    public static void Clear(HttpContext context)
    {
        context.Response.Cookies.Delete(Access, new CookieOptions { HttpOnly = true, Path = "/" });
        context.Response.Cookies.Delete(Refresh, new CookieOptions { HttpOnly = true, Path = "/" });
    }
}
