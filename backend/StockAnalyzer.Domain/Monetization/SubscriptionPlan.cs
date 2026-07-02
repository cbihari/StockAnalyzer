namespace StockAnalyzer.Domain.Monetization;

public static class SubscriptionPlan
{
    public const string Free = "free";
    public const string Pro = "pro";
    public const string Power = "power";

    public static readonly string[] Supported = [Free, Pro, Power];

    public static string Normalize(string? value)
    {
        var normalized = (value ?? Free).Trim().ToLowerInvariant();
        return Supported.Contains(normalized, StringComparer.Ordinal) ? normalized : Free;
    }
}
