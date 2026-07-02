using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Abstractions;

public interface IMonetizationService
{
    Task<MonetizationStatusDto> GetStatusAsync(
        Guid? userId,
        string clientId,
        CancellationToken cancellationToken);

    Task<UsageCheckDto> CheckAsync(
        Guid? userId,
        string clientId,
        string featureKey,
        int quantity,
        CancellationToken cancellationToken);

    Task<UsageCheckDto> RecordAsync(
        Guid? userId,
        string clientId,
        string featureKey,
        int quantity,
        CancellationToken cancellationToken);

    Task<CheckoutResponseDto> StartCheckoutAsync(
        Guid userId,
        CheckoutRequestDto request,
        CancellationToken cancellationToken);

    Task<PaymentWebhookResultDto> HandleWebhookAsync(
        string provider,
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken);
}
