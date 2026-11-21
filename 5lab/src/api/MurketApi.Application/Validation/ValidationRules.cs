using System.Text.RegularExpressions;

using MurketApi.Domain.Common;
using MurketApi.Domain.Orders;
using MurketApi.Domain.Security;
using MurketApi.Domain.Users;

namespace MurketApi.Application.Validation;

/// <summary>Канонические правила валидации запросов (тексты — спецификация §8).</summary>
public sealed partial class ValidationRules
{
    public const string DefaultMessage = "Данные заполнены неверно";

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$")]
    private static partial Regex EmailRegex();

    /// <summary>Сборщик ошибок полей → ValidationException с конвертом {message, errors}.</summary>
    public sealed class Errors
    {
        private readonly Dictionary<string, string[]> fields = [];

        public void Add(string field, string message) => Add(field, [message]);

        public void Add(string field, string[] messages)
        {
            if (fields.TryGetValue(field, out var existing))
            {
                fields[field] = [.. existing, .. messages];
            }
            else
            {
                fields[field] = messages;
            }
        }

        public bool Any => fields.Count > 0;

        public ValidationException ToException(string message = DefaultMessage) => new(message, fields);
    }

    public static bool IsValidEmail(string? email) =>
        email is not null && EmailRegex().IsMatch(email.Trim());

    public static string NormalizeEmail(string? email) => (email ?? string.Empty).Trim().ToLowerInvariant();

    // ----- Восстановление доступа: контакт -----

    public static RecoveryChannel DetectChannel(string contact) =>
        contact.Contains('@') ? RecoveryChannel.Email : RecoveryChannel.Phone;

    public static string NormalizeContact(string contact, RecoveryChannel channel) =>
        channel == RecoveryChannel.Email ? NormalizeEmail(contact) : Phone.Normalize(contact);

    public static bool IsValidContact(string contact, RecoveryChannel channel) =>
        channel == RecoveryChannel.Email
            ? IsValidEmail(contact)
            : Phone.IsValidE164(Phone.Normalize(contact));

    /// <summary>Маскирует контакт: «an***@mail.ru» / «+799912 ***-**-67».</summary>
    public static string MaskContact(string contact, RecoveryChannel channel)
    {
        if (channel == RecoveryChannel.Email)
        {
            var at = contact.IndexOf('@');
            var name = at > 0 ? contact[..at] : contact;
            var domain = at > 0 ? contact[(at + 1)..] : string.Empty;
            var visible = name[..Math.Min(3, name.Length)];
            return $"{visible}{new string('•', Math.Max(3, name.Length - visible.Length))}@{domain}";
        }

        var phone = Phone.Normalize(contact);
        return phone.Length == 12 ? $"{phone[..7]} ***-**-{phone[^2..]}" : "***";
    }

    // ----- Пароль -----

    public static void ValidatePassword(Errors errors, string? password, string field = "password")
    {
        var issues = PasswordPolicy.Issues(password);
        if (issues.Count > 0)
        {
            errors.Add(field, issues.Select(issue => $"Пароль: {issue}").ToArray());
        }
    }

    // ----- Способ доставки / статус заказа -----

    public static DeliveryMethod? TryParseDelivery(string? value) => value switch
    {
        "courier" => DeliveryMethod.Courier,
        "pickup" => DeliveryMethod.Pickup,
        _ => null,
    };

    public static OrderStatus? TryParseStatus(string? value) => value switch
    {
        "new" => OrderStatus.New,
        "confirmed" => OrderStatus.Confirmed,
        "delivered" => OrderStatus.Delivered,
        "cancelled" => OrderStatus.Cancelled,
        _ => null,
    };
}
