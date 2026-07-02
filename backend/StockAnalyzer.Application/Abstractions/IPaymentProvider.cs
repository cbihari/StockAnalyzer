using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Abstractions;

public interface IPaymentProvider
{
    string Name { get; }

    Task<PaymentCheckoutSessionDto> CreateCheckoutSessionAsync(
        Guid userId,
        string planKey,
        string successUrl,
        string cancelUrl,
        CancellationToken cancellationToken);

    Task<PaymentWebhookEventDto> ParseWebhookAsync(
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken);
}
