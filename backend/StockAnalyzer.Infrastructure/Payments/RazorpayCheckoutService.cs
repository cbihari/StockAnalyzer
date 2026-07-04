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

public sealed class RazorpayCheckoutService(
    HttpClient httpClient,
    RazorpaySettings settings,
    ISubscriptionRepository subscriptionRepository) : IRazorpayCheckoutService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string Provider = "razorpay";

    public async Task<RazorpayOrderResponseDto> CreateOrderAsync(
        Guid userId,
        string userName,
        string userEmail,
        RazorpayOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        settings.EnsureApiCredentials();
        var planKey = SubscriptionPlan.Normalize(request.PlanKey);
        var amount = PlanAmountPaise(planKey);
        var receipt = $"sa_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}"[..40];
        using var orderRequest = new HttpRequestMessage(HttpMethod.Post, "orders")
        {
            Content = JsonContent.Create(new
            {
                amount,
                currency = settings.Currency,
                receipt,
                notes = new Dictionary<string, string>
                {
                    ["user_id"] = userId.ToString(),
                    ["plan_key"] = planKey,
                    ["mode"] = "test"
                }
            }, options: JsonOptions)
        };
        orderRequest.Headers.Authorization = BuildBasicAuth(settings.KeyId, settings.KeySecret);

        using var response = await httpClient.SendAsync(orderRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Razorpay order creation failed.");
        }

        var order = DeserializeOrder(body);
        var subscription = await subscriptionRepository.GetCurrentForUserAsync(userId, cancellationToken)
            ?? new UserSubscription
            {
                UserId = userId,
                PlanKey = planKey,
                Status = SubscriptionStatus.Pending,
                Provider = Provider
            };

        subscription.PlanKey = planKey;
        subscription.Status = SubscriptionStatus.Pending;
        subscription.Provider = Provider;
        subscription.ProviderCheckoutSessionId = order.Id;
        subscription.ProviderSubscriptionId = null;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        await subscriptionRepository.SaveAsync(subscription, cancellationToken);

        return new RazorpayOrderResponseDto(
            Provider,
            settings.KeyId,
            order.Id,
            order.Amount,
            order.Currency,
            planKey,
            "StockAnalyzer",
            $"StockAnalyzer {PlanName(planKey)} plan",
            string.IsNullOrWhiteSpace(userName) ? "StockAnalyzer user" : userName,
            userEmail,
            "Razorpay test order created.");
    }

    public async Task<RazorpayPaymentVerificationResponseDto> VerifyPaymentAsync(
        Guid userId,
        RazorpayPaymentVerificationRequestDto request,
        CancellationToken cancellationToken)
    {
        settings.EnsureApiCredentials();
        var paymentId = Required(request.RazorpayPaymentId, "Razorpay payment ID is required.");
        var signature = Required(request.RazorpaySignature, "Razorpay signature is required.");
        var orderId = request.RazorpayOrderId?.Trim();
        var subscriptionId = request.RazorpaySubscriptionId?.Trim();
        if (string.IsNullOrWhiteSpace(orderId) && string.IsNullOrWhiteSpace(subscriptionId))
        {
            throw new ArgumentException("Razorpay order ID or subscription ID is required.", nameof(request));
        }

        var subscription = await subscriptionRepository.FindByProviderReferenceAsync(
                Provider,
                orderId,
                subscriptionId,
                cancellationToken)
            ?? throw new ArgumentException("Subscription was not found for this Razorpay payment.", nameof(request));
        if (subscription.UserId != userId)
        {
            throw new UnauthorizedAccessException("Razorpay payment does not belong to the signed-in user.");
        }

        var serverOrderId = subscription.ProviderCheckoutSessionId;
        var serverSubscriptionId = subscription.ProviderSubscriptionId ?? subscriptionId;
        var signaturePayload = string.IsNullOrWhiteSpace(serverOrderId)
            ? $"{paymentId}|{serverSubscriptionId}"
            : $"{serverOrderId}|{paymentId}";
        if (!IsValidSignature(signaturePayload, signature, settings.KeySecret))
        {
            throw new ArgumentException("Razorpay payment signature is invalid.", nameof(request));
        }

        var payment = await GetPaymentAsync(paymentId, cancellationToken);
        if (!string.Equals(payment.Status, "captured", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Razorpay payment is not captured.", nameof(request));
        }

        if (!string.IsNullOrWhiteSpace(serverOrderId) &&
            !string.Equals(payment.OrderId, serverOrderId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Razorpay payment does not match the stored order.", nameof(request));
        }

        if (payment.Amount != PlanAmountPaise(subscription.PlanKey) ||
            !string.Equals(payment.Currency, settings.Currency, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Razorpay payment amount or currency does not match the selected plan.", nameof(request));
        }

        subscription.Status = SubscriptionStatus.Active;
        subscription.Provider = Provider;
        subscription.ProviderSubscriptionId = string.IsNullOrWhiteSpace(subscriptionId) ? paymentId : subscriptionId;
        subscription.ProviderCheckoutSessionId = serverOrderId ?? subscription.ProviderCheckoutSessionId;
        subscription.CurrentPeriodStart = DateTimeOffset.UtcNow;
        subscription.CurrentPeriodEnd = DateTimeOffset.UtcNow.AddMonths(1);
        subscription.CancelAtPeriodEnd = false;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        await subscriptionRepository.SaveAsync(subscription, cancellationToken);

        return new RazorpayPaymentVerificationResponseDto(
            Provider,
            subscription.PlanKey,
            subscription.Status,
            subscription.ProviderCheckoutSessionId ?? subscription.ProviderSubscriptionId ?? string.Empty,
            "Payment verified. Your plan is active.");
    }

    private int PlanAmountPaise(string planKey) => planKey switch
    {
        SubscriptionPlan.Pro => settings.ProAmountPaise,
        SubscriptionPlan.Power => settings.PowerAmountPaise,
        _ => throw new ArgumentException("Razorpay checkout is available only for paid plans.", nameof(planKey))
    };

    private async Task<RazorpayPaymentApiResponse> GetPaymentAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        using var paymentRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"payments/{Uri.EscapeDataString(paymentId)}");
        paymentRequest.Headers.Authorization = BuildBasicAuth(settings.KeyId, settings.KeySecret);

        using var response = await httpClient.SendAsync(paymentRequest, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Razorpay payment verification request failed.");
        }

        try
        {
            var payment = JsonSerializer.Deserialize<RazorpayPaymentApiResponse>(body, JsonOptions);
            if (payment is null || string.IsNullOrWhiteSpace(payment.Id))
            {
                throw new InvalidOperationException("Razorpay payment response was incomplete.");
            }

            return payment;
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Razorpay payment response was invalid.", exception);
        }
    }

    private static RazorpayOrderApiResponse DeserializeOrder(string body)
    {
        try
        {
            var order = JsonSerializer.Deserialize<RazorpayOrderApiResponse>(body, JsonOptions);
            if (order is null || string.IsNullOrWhiteSpace(order.Id))
            {
                throw new InvalidOperationException("Razorpay order response was incomplete.");
            }

            return order;
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Razorpay order response was invalid.", exception);
        }
    }

    private static string Required(string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message);
        }

        return value.Trim();
    }

    private static bool IsValidSignature(string payload, string signature, string keySecret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(keySecret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant()));
    }

    private static AuthenticationHeaderValue BuildBasicAuth(string keyId, string keySecret)
    {
        var raw = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));
        return new AuthenticationHeaderValue("Basic", raw);
    }

    private static string PlanName(string planKey) =>
        planKey == SubscriptionPlan.Power ? "Power" : "Pro";

    private sealed record RazorpayOrderApiResponse(
        string Id,
        int Amount,
        string Currency);

    private sealed record RazorpayPaymentApiResponse(
        string Id,
        [property: JsonPropertyName("order_id")]
        string? OrderId,
        int Amount,
        string Currency,
        string Status);
}
