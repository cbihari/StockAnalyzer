using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Domain.Monetization;

namespace StockAnalyzer.Infrastructure.Payments;

public sealed class RazorpayPaymentProvider(
    HttpClient httpClient,
    RazorpaySettings settings) : IPaymentProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Name => "razorpay";

    public async Task<PaymentCheckoutSessionDto> CreateCheckoutSessionAsync(
        Guid userId,
        string planKey,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken)
    {
        var settings = ReadSettings();
        var amount = PlanAmountPaise(planKey);
        using var request = new HttpRequestMessage(HttpMethod.Post, "payment_links")
        {
            Content = JsonContent.Create(new
            {
                amount,
                currency = settings.Currency,
                accept_partial = false,
                reference_id = $"sa_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..32],
                description = $"StockAnalyzer {SubscriptionPlan.Normalize(planKey)} plan",
                callback_url = successUrl,
                callback_method = "get",
                notes = new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString(),
                    ["plan_key"] = SubscriptionPlan.Normalize(planKey),
                    ["cancel_url"] = cancelUrl
                }
            }, options: JsonOptions)
        };
        request.Headers.Authorization = BuildBasicAuth(settings.KeyId, settings.KeySecret);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Razorpay payment link creation failed.");
        }

        RazorpayPaymentLinkResponse? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<RazorpayPaymentLinkResponse>(body, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Razorpay payment link response was invalid.", exception);
        }

        if (parsed is null || string.IsNullOrWhiteSpace(parsed.Id) || string.IsNullOrWhiteSpace(parsed.ShortUrl))
        {
            throw new InvalidOperationException("Razorpay payment link response was incomplete.");
        }

        return new PaymentCheckoutSessionDto(
            Name,
            parsed.Id,
            parsed.ShortUrl);
    }

    public Task<PaymentWebhookEventDto> ParseWebhookAsync(
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        var settings = ReadSettings();
        VerifySignature(payload, headers, settings.WebhookSecret);
        RazorpayWebhookPayload? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<RazorpayWebhookPayload>(payload, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Razorpay webhook payload is invalid JSON.", nameof(payload), exception);
        }

        if (parsed is null)
        {
            throw new ArgumentException("Razorpay webhook payload is required.", nameof(payload));
        }

        var paymentLink = parsed.Payload?.PaymentLink?.Entity;
        var payment = parsed.Payload?.Payment?.Entity;
        var planKey = FirstNonEmpty(paymentLink?.Notes?.GetValueOrDefault("plan_key"), payment?.Notes?.GetValueOrDefault("plan_key"));
        var status = ResolveStatus(parsed.Event, paymentLink?.Status, payment?.Status);
        return Task.FromResult(new PaymentWebhookEventDto(
            string.IsNullOrWhiteSpace(parsed.EventId) ? $"razorpay_evt_{Guid.NewGuid():N}" : parsed.EventId.Trim(),
            string.IsNullOrWhiteSpace(parsed.Event) ? $"razorpay.{status}" : parsed.Event.Trim(),
            status,
            string.IsNullOrWhiteSpace(planKey) ? null : SubscriptionPlan.Normalize(planKey),
            FirstNonEmpty(paymentLink?.Id, paymentLink?.ReferenceId),
            payment?.Id,
            FirstNonEmpty(paymentLink?.CustomerId, payment?.CustomerId),
            DateTimeOffset.UtcNow,
            status == SubscriptionStatus.Active ? DateTimeOffset.UtcNow.AddMonths(1) : null,
            false));
    }

    private RazorpaySettings ReadSettings()
    {
        settings.EnsureApiCredentials();
        settings.EnsureWebhookSecret();
        return settings;
    }

    private int PlanAmountPaise(string planKey)
    {
        var normalized = SubscriptionPlan.Normalize(planKey);
        var configured = Environment.GetEnvironmentVariable($"RAZORPAY_{normalized.ToUpperInvariant()}_AMOUNT_PAISE");
        if (int.TryParse(configured, out var envAmount) && envAmount > 0)
        {
            return envAmount;
        }

        return normalized switch
        {
            SubscriptionPlan.Pro => settings.ProAmountPaise,
            SubscriptionPlan.Power => settings.PowerAmountPaise,
            _ => throw new ArgumentException("Razorpay checkout is available only for paid plans.", nameof(planKey))
        };
    }

    private static AuthenticationHeaderValue BuildBasicAuth(string keyId, string keySecret)
    {
        var raw = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));
        return new AuthenticationHeaderValue("Basic", raw);
    }

    private static void VerifySignature(
        string payload,
        IReadOnlyDictionary<string, string> headers,
        string webhookSecret)
    {
        var signature = headers.FirstOrDefault(header =>
            string.Equals(header.Key, "X-Razorpay-Signature", StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(signature.Value))
        {
            throw new ArgumentException("Razorpay webhook signature is missing.", nameof(headers));
        }

        var secret = Encoding.UTF8.GetBytes(webhookSecret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(secret);
        var expected = Convert.ToHexString(hmac.ComputeHash(payloadBytes)).ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(signature.Value.Trim().ToLowerInvariant())))
        {
            throw new ArgumentException("Razorpay webhook signature is invalid.", nameof(headers));
        }
    }

    private static string ResolveStatus(string? eventName, string? paymentLinkStatus, string? paymentStatus)
    {
        if (string.Equals(eventName, "payment_link.paid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(paymentLinkStatus, "paid", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(paymentStatus, "captured", StringComparison.OrdinalIgnoreCase))
        {
            return SubscriptionStatus.Active;
        }

        if (string.Equals(paymentStatus, "failed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(paymentLinkStatus, "cancelled", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(paymentLinkStatus, "expired", StringComparison.OrdinalIgnoreCase))
        {
            return SubscriptionStatus.Canceled;
        }

        return SubscriptionStatus.Pending;
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.Select(value => value?.Trim()).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private sealed record RazorpayPaymentLinkResponse(
        string Id,
        [property: JsonPropertyName("short_url")]
        string ShortUrl);

    private sealed record RazorpayWebhookPayload(
        [property: JsonPropertyName("event_id")]
        string? EventId,
        string? Event,
        RazorpayWebhookPayloadEntities? Payload);

    private sealed record RazorpayWebhookPayloadEntities(
        [property: JsonPropertyName("payment_link")]
        RazorpayEntityEnvelope<RazorpayPaymentLinkEntity>? PaymentLink,
        RazorpayEntityEnvelope<RazorpayPaymentEntity>? Payment);

    private sealed record RazorpayEntityEnvelope<T>(T? Entity);

    private sealed record RazorpayPaymentLinkEntity(
        string? Id,
        [property: JsonPropertyName("reference_id")]
        string? ReferenceId,
        string? Status,
        [property: JsonPropertyName("customer_id")]
        string? CustomerId,
        IReadOnlyDictionary<string, string>? Notes);

    private sealed record RazorpayPaymentEntity(
        string? Id,
        string? Status,
        [property: JsonPropertyName("customer_id")]
        string? CustomerId,
        IReadOnlyDictionary<string, string>? Notes);
}
