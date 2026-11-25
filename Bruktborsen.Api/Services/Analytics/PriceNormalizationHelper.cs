using Bruktborsen.Api.Models.Enums;

namespace Bruktborsen.Api.Services.Analytics;

public static class PriceNormalizationHelper
{
    public static decimal GetConditionFactor(Condition condition)
    {
        return condition switch
        {
            Condition.New => 1.00m,
            Condition.LikeNew => 0.85m,
            Condition.Good => 0.70m,
            Condition.Used => 0.55m,
            Condition.Defect => 0.30m,

            _ => throw new ArgumentOutOfRangeException(
                nameof(condition),
                "Ugyldig tilstand.")
        };
    }

    public static decimal NormalizePrice(
        decimal price,
        Condition condition)
    {
        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price));

        return price / GetConditionFactor(condition);
    }

    public static decimal DenormalizePrice(
        decimal normalizedPrice,
        Condition condition)
    {
        if (normalizedPrice <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(normalizedPrice));

        return normalizedPrice * GetConditionFactor(condition);
    }
}