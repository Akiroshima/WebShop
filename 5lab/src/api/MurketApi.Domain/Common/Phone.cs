using System.Diagnostics.CodeAnalysis;

namespace MurketApi.Domain.Common;

/// <summary>Телефон: нормализация ввода к E.164 (+7…).</summary>
public static class Phone
{
    /// <summary>«8 999 123-45-67» и «+7 (999) 123-45-67» → «+79991234567».</summary>
    public static string Normalize(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            return string.Empty;
        }

        if (digits.Length == 11 && digits[0] == '8')
        {
            return "+7" + digits[1..];
        }

        return "+" + digits;
    }

    /// <summary>Российский номер в формате E.164: +7 и 10 цифр.</summary>
    public static bool IsValidE164([NotNullWhen(true)] string? value)
        => value is not null && value.Length == 12 && value.StartsWith("+7") && value[2..].All(char.IsDigit);
}
