namespace MurketApi.Domain.Common;

/// <summary>Базовый тип доменных ошибок: текст сообщения — канонический текст системы.</summary>
public abstract class DomainException(string message) : Exception(message)
{
}

/// <summary>Ошибка валидации входных данных: 400 + конверт {message, errors}.</summary>
public sealed class ValidationException(string message, IReadOnlyDictionary<string, string[]>? errors = null)
    : DomainException(message)
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors ?? new Dictionary<string, string[]>();
}

/// <summary>Требуется аутентификация: 401.</summary>
public sealed class UnauthorizedException(string message = "Требуется вход в аккаунт") : DomainException(message)
{
}

/// <summary>Роль не подходит: 403.</summary>
public sealed class ForbiddenException(string message = "Доступ запрещён") : DomainException(message)
{
}

/// <summary>Сущность не найдена (включая чужие заказы и адреса — существование не раскрывается): 404.</summary>
public sealed class NotFoundException(string message = "Не найдено") : DomainException(message)
{
}

/// <summary>Бизнес-конфликт (дубликат, остатки, недопустимый переход): 409.</summary>
public sealed class ConflictException(string message) : DomainException(message)
{
}

/// <summary>Превышен лимит попыток: 429.</summary>
public sealed class RateLimitException(string message = "Слишком много попыток. Повторите позже")
    : DomainException(message)
{
}
