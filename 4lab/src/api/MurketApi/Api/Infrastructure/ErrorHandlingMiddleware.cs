using System.Text.Json;

using MurketApi.Domain.Common;

namespace MurketApi.Api.Infrastructure;

/// <summary>Запись единого конверта ошибок {message, errors?} в ответ.</summary>
public static class ErrorEnvelope
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public static async Task WriteAsync(HttpResponse response, int status, string message,
        IReadOnlyDictionary<string, string[]>? errors = null)
    {
        if (response.HasStarted)
        {
            return;
        }

        response.StatusCode = status;
        response.ContentType = "application/json; charset=utf-8";
        await response.WriteAsync(JsonSerializer.Serialize(new { message, errors }, JsonOptions));
    }
}

/// <summary>
/// Единый конверт ошибок: доменные исключения → HTTP-статусы; тело всегда {message, errors?}.
/// Непредвиденные ошибки — 500 с общим текстом (детали только в журнал, наружу не утекают).
/// </summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            var (status, errors) = exception switch
            {
                ValidationException validation => (StatusCodes.Status400BadRequest, Map(validation.Errors)),
                UnauthorizedException => (StatusCodes.Status401Unauthorized, null),
                ForbiddenException => (StatusCodes.Status403Forbidden, null),
                NotFoundException => (StatusCodes.Status404NotFound, null),
                ConflictException => (StatusCodes.Status409Conflict, null),
                RateLimitException => (StatusCodes.Status429TooManyRequests, null),
                _ => (StatusCodes.Status400BadRequest, null),
            };
            await WriteAsync(context, status, exception.Message, errors);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            logger.LogError(exception, "Необработанная ошибка: {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError,
                "Внутренняя ошибка сервера. Попробуйте позже");
        }
    }

    private static Dictionary<string, string[]> Map(IReadOnlyDictionary<string, string[]> errors) =>
        errors.ToDictionary(pair => pair.Key, pair => pair.Value);

    private static Task WriteAsync(HttpContext context, int status, string message,
        IReadOnlyDictionary<string, string[]>? errors = null) =>
        ErrorEnvelope.WriteAsync(context.Response, status, message, errors);
}
