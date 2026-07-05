using Microsoft.Extensions.Configuration;

namespace StockAnalyzer.Infrastructure.Payments;

public sealed class RazorpaySettings
{
    public const string TestMode = "test";
    public const string LiveMode = "live";

    public string Mode { get; init; } = TestMode;
    public string KeyId { get; init; } = string.Empty;
    public string KeySecret { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = "https://api.razorpay.com/v1/";
    public string Currency { get; init; } = "INR";
    public int ProAmountPaise { get; init; } = 49900;
    public int PowerAmountPaise { get; init; } = 99900;

    public static RazorpaySettings FromConfiguration(IConfiguration configuration)
    {
        var legacy = configuration.GetSection("Payments:Razorpay");
        var direct = configuration.GetSection("Razorpay");
        return new RazorpaySettings
        {
            Mode = FirstNonEmpty(
                Environment.GetEnvironmentVariable("RAZORPAY_MODE"),
                direct["Mode"],
                legacy["Mode"],
                TestMode)!,
            KeyId = FirstNonEmpty(
                Environment.GetEnvironmentVariable("RAZORPAY_KEY_ID"),
                direct["KeyId"],
                legacy["KeyId"]) ?? string.Empty,
            KeySecret = FirstNonEmpty(
                Environment.GetEnvironmentVariable("RAZORPAY_KEY_SECRET"),
                direct["KeySecret"],
                legacy["KeySecret"]) ?? string.Empty,
            WebhookSecret = FirstNonEmpty(
                Environment.GetEnvironmentVariable("RAZORPAY_WEBHOOK_SECRET"),
                direct["WebhookSecret"],
                legacy["WebhookSecret"]) ?? string.Empty,
            BaseUrl = FirstNonEmpty(
                direct["BaseUrl"],
                legacy["BaseUrl"],
                "https://api.razorpay.com/v1/")!,
            Currency = FirstNonEmpty(
                Environment.GetEnvironmentVariable("RAZORPAY_CURRENCY"),
                direct["Currency"],
                legacy["Currency"],
                "INR")!,
            ProAmountPaise = PositiveInt(
                Environment.GetEnvironmentVariable("RAZORPAY_PRO_AMOUNT_PAISE"),
                direct["ProAmountPaise"],
                legacy["ProAmountPaise"],
                49900),
            PowerAmountPaise = PositiveInt(
                Environment.GetEnvironmentVariable("RAZORPAY_POWER_AMOUNT_PAISE"),
                direct["PowerAmountPaise"],
                legacy["PowerAmountPaise"],
                99900)
        };
    }

    public string NormalizedMode => NormalizeMode(Mode);

    public void EnsureApiCredentials()
    {
        if (string.IsNullOrWhiteSpace(KeyId) || string.IsNullOrWhiteSpace(KeySecret))
        {
            throw new InvalidOperationException("Razorpay key ID and key secret are required.");
        }

        var mode = NormalizedMode;
        if (mode == TestMode && !KeyId.StartsWith("rzp_test_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Razorpay test mode requires a key ID that starts with rzp_test_. Set RAZORPAY_MODE=live only when using live production keys.");
        }

        if (mode == LiveMode && !KeyId.StartsWith("rzp_live_", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Razorpay live mode requires a key ID that starts with rzp_live_. Use RAZORPAY_MODE=test for test keys.");
        }
    }

    public void EnsureWebhookSecret()
    {
        if (string.IsNullOrWhiteSpace(WebhookSecret))
        {
            throw new InvalidOperationException("Razorpay webhook secret is required.");
        }
    }

    private static int PositiveInt(string? first, string? second, string? third, int fallback)
    {
        foreach (var value in new[] { first, second, third })
        {
            if (int.TryParse(value, out var parsed) && parsed > 0)
            {
                return parsed;
            }
        }

        return fallback;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.Select(value => value?.Trim()).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static string NormalizeMode(string? mode)
    {
        var normalized = string.IsNullOrWhiteSpace(mode)
            ? TestMode
            : mode.Trim().ToLowerInvariant();
        if (normalized is not TestMode and not LiveMode)
        {
            throw new InvalidOperationException("Razorpay mode must be either test or live.");
        }

        return normalized;
    }
}
