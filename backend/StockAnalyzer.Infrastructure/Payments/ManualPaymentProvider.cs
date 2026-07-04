using System.Text.Json;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Domain.Monetization;

namespace StockAnalyzer.Infrastructure.Payments;

public sealed class ManualPaymentProvider : IPaymentProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string Name => "manual";

    public Task<PaymentCheckoutSessionDto> CreateCheckoutSessionAsync(
        Guid userId,
        string planKey,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken)
    {
        var sessionId = $"manual_{Guid.NewGuid():N}";
        var checkoutUrl = BuildCheckoutUrl(successUrl, sessionId, planKey);
        return Task.FromResult(new PaymentCheckoutSessionDto(
            Name,
            sessionId,
            checkoutUrl));
    }

    public Task<PaymentWebhookEventDto> ParseWebhookAsync(
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        ManualWebhookPayload? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<ManualWebhookPayload>(payload, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("Manual payment webhook payload is invalid JSON.", nameof(payload), exception);
        }

        if (parsed is null)
        {
            throw new ArgumentException("Manual payment webhook payload is required.", nameof(payload));
        }

        var checkoutSessionId = FirstNonEmpty(
            parsed.ProviderCheckoutSessionId,
            parsed.ProviderSessionId,
            parsed.CheckoutSessionId);
        var subscriptionId = FirstNonEmpty(parsed.ProviderSubscriptionId, parsed.SubscriptionId);
        if (string.IsNullOrWhiteSpace(checkoutSessionId) && string.IsNullOrWhiteSpace(subscriptionId))
        {
            throw new ArgumentException(
                "Manual payment webhook requires a provider checkout session ID or subscription ID.",
                nameof(payload));
        }

        var status = SubscriptionStatus.Normalize(parsed.Status);
        return Task.FromResult(new PaymentWebhookEventDto(
            string.IsNullOrWhiteSpace(parsed.EventId) ? $"manual_evt_{Guid.NewGuid():N}" : parsed.EventId.Trim(),
            string.IsNullOrWhiteSpace(parsed.EventType) ? $"subscription.{status}" : parsed.EventType.Trim(),
            status,
            string.IsNullOrWhiteSpace(parsed.PlanKey) ? null : SubscriptionPlan.Normalize(parsed.PlanKey),
            checkoutSessionId,
            subscriptionId,
            FirstNonEmpty(parsed.ProviderCustomerId, parsed.CustomerId),
            parsed.CurrentPeriodStart,
            parsed.CurrentPeriodEnd,
            parsed.CancelAtPeriodEnd));
    }

    private static string BuildCheckoutUrl(string successUrl, string sessionId, string planKey)
    {
        var separator = successUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return string.Concat(
            successUrl,
            separator,
            "checkoutSession=",
            Uri.EscapeDataString(sessionId),
            "&plan=",
            Uri.EscapeDataString(planKey),
            "&provider=manual");
    }

    private static string? FirstNonEmpty(params string?[] values) =>
        values.Select(value => value?.Trim()).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private sealed record ManualWebhookPayload(
        string? EventId,
        string? EventType,
        string? Status,
        string? PlanKey,
        string? ProviderSessionId,
        string? CheckoutSessionId,
        string? ProviderCheckoutSessionId,
        string? ProviderSubscriptionId,
        string? SubscriptionId,
        string? ProviderCustomerId,
        string? CustomerId,
        DateTimeOffset? CurrentPeriodStart,
        DateTimeOffset? CurrentPeriodEnd,
        bool CancelAtPeriodEnd);
}
