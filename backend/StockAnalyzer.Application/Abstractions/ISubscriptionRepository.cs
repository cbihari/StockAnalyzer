using StockAnalyzer.Domain.Monetization;

namespace StockAnalyzer.Application.Abstractions;

public interface ISubscriptionRepository
{
    Task<UserSubscription?> GetCurrentForUserAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<UserSubscription?> FindByProviderReferenceAsync(
        string provider,
        string? providerCheckoutSessionId,
        string? providerSubscriptionId,
        CancellationToken cancellationToken);

    Task SaveAsync(
        UserSubscription subscription,
        CancellationToken cancellationToken);
}
