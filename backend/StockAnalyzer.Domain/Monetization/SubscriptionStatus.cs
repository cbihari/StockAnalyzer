namespace StockAnalyzer.Domain.Monetization;

public static class SubscriptionStatus
{
    public const string Pending = "pending";
    public const string Active = "active";
    public const string PastDue = "past_due";
    public const string Canceled = "canceled";

    public static readonly string[] Supported = [Pending, Active, PastDue, Canceled];

    public static string Normalize(string? value)
    {
        var normalized = (value ?? Pending).Trim().ToLowerInvariant();
        return Supported.Contains(normalized, StringComparer.Ordinal) ? normalized : Pending;
    }
}
