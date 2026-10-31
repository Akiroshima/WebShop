using MurketApi.Domain.Common;

namespace MurketApi.Domain.Promos;

public enum PromoType
{
    Percent = 0,
    Fixed,
}

/// <summary>Промокод: не более одного на корзину и заказ; код хранится в верхнем регистре.</summary>
public sealed class PromoCode
{
    public string Id { get; private set; } = string.Empty;

    public string Code { get; private set; } = string.Empty;

    public PromoType Type { get; private set; }

    /// <summary>Процент (≤ 100) либо фиксированная сумма скидки.</summary>
    public decimal Value { get; private set; }

    /// <summary>Минимальная сумма товаров для применения.</summary>
    public decimal MinSubtotal { get; private set; }

    public DateTimeOffset StartsAt { get; private set; }

    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>Лимит использований (null — без лимита).</summary>
    public int? MaxUses { get; private set; }

    public int UsedCount { get; private set; }

    public bool IsActive { get; private set; }

    private PromoCode()
    {
    }

    public static PromoCode Create(string id, string code, PromoType type, decimal value, decimal minSubtotal,
        DateTimeOffset startsAt, DateTimeOffset endsAt, int? maxUses, int usedCount, bool isActive)
    {
        var errors = new Dictionary<string, string[]>();
        var normalized = (code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length is < 1 or > 50)
        {
            errors["code"] = ["Код — от 1 до 50 символов"];
        }

        if (value <= 0 || (type == PromoType.Percent && value > 100))
        {
            errors["value"] = ["Значение скидки должно быть положительным (процент — не больше 100)"];
        }

        if (minSubtotal < 0)
        {
            errors["minSubtotal"] = ["Минимальная сумма не может быть отрицательной"];
        }

        if (endsAt <= startsAt)
        {
            errors["endsAt"] = ["Окончание действия должно быть позже начала"];
        }

        if (maxUses is < 0)
        {
            errors["maxUses"] = ["Лимит использований не может быть отрицательным"];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Данные заполнены неверно", errors);
        }

        return new PromoCode
        {
            Id = id,
            Code = normalized,
            Type = type,
            Value = value,
            MinSubtotal = minSubtotal,
            StartsAt = startsAt,
            EndsAt = endsAt,
            MaxUses = maxUses,
            UsedCount = usedCount,
            IsActive = isActive,
        };
    }

    /// <summary>Промокод действует прямо сейчас (активен, период, лимит).</summary>
    public bool IsUsable(DateTimeOffset now) => IsActive && StartsAt <= now && now <= EndsAt
        && (MaxUses is null || UsedCount < MaxUses);

    /// <summary>Скидка от суммы товаров; не больше самой суммы. Округление — до копеек.</summary>
    public decimal DiscountFor(decimal subtotal)
    {
        var raw = Type == PromoType.Percent ? subtotal * Value / 100m : Value;
        return Money.Round(Math.Min(Money.Round(raw), subtotal));
    }

    /// <summary>Фиксирует использование (при оформлении заказа).</summary>
    public void RegisterUse() => UsedCount++;
}
