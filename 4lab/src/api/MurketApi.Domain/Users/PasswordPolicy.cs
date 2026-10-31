using System.Text.RegularExpressions;

namespace MurketApi.Domain.Users;

/// <summary>Правила пароля (спецификация §8): 8–128 символов, цифра, буква, спецзнак.</summary>
public static partial class PasswordPolicy
{
    public const int MinLength = 8;

    public const int MaxLength = 128;

    [GeneratedRegex(@"[0-9]")]
    private static partial Regex DigitRegex();

    [GeneratedRegex(@"[A-Za-zА-Яа-яЁё]")]
    private static partial Regex LetterRegex();

    [GeneratedRegex(@"[^A-Za-zА-Яа-яЁё0-9]")]
    private static partial Regex SpecialRegex();

    /// <summary>Список нарушений правил (пустой — пароль подходит).</summary>
    public static IReadOnlyList<string> Issues(string? password)
    {
        var issues = new List<string>();
        var value = password ?? string.Empty;
        if (value.Length is < MinLength or > MaxLength)
        {
            issues.Add($"от {MinLength} до {MaxLength} символов");
        }

        if (!DigitRegex().IsMatch(value))
        {
            issues.Add("хотя бы одна цифра");
        }

        if (!LetterRegex().IsMatch(value))
        {
            issues.Add("хотя бы одна буква");
        }

        if (!SpecialRegex().IsMatch(value))
        {
            issues.Add("хотя бы один спецзнак");
        }

        return issues;
    }

    public static bool IsAcceptable(string? password) => Issues(password).Count == 0;
}
