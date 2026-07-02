using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Domain.Monetization;

namespace StockAnalyzer.Infrastructure.Persistence.Repositories;

public sealed class SubscriptionRepository(StockAnalyzerDbContext dbContext) : ISubscriptionRepository
{
    public Task<UserSubscription?> GetCurrentForUserAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.UserSubscriptions
            .AsNoTracking()
            .Where(subscription => subscription.UserId == userId)
            .OrderByDescending(subscription => subscription.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<UserSubscription?> FindByProviderReferenceAsync(
        string provider,
        string? providerCheckoutSessionId,
        string? providerSubscriptionId,
        CancellationToken cancellationToken)
    {
        var normalizedProvider = provider.Trim().ToLowerInvariant();
        var checkoutSessionId = providerCheckoutSessionId?.Trim();
        var subscriptionId = providerSubscriptionId?.Trim();
        if (string.IsNullOrWhiteSpace(checkoutSessionId) && string.IsNullOrWhiteSpace(subscriptionId))
        {
            return Task.FromResult<UserSubscription?>(null);
        }

        return dbContext.UserSubscriptions
            .Where(subscription => subscription.Provider == normalizedProvider)
            .Where(subscription =>
                (!string.IsNullOrWhiteSpace(subscriptionId) &&
                    subscription.ProviderSubscriptionId == subscriptionId) ||
                (!string.IsNullOrWhiteSpace(checkoutSessionId) &&
                    subscription.ProviderCheckoutSessionId == checkoutSessionId))
            .OrderByDescending(subscription => subscription.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task SaveAsync(
        UserSubscription subscription,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.UserSubscriptions.SingleOrDefaultAsync(
            item => item.Id == subscription.Id,
            cancellationToken);
        if (existing is null)
        {
            dbContext.UserSubscriptions.Add(subscription);
        }
        else
        {
            existing.PlanKey = subscription.PlanKey;
            existing.Status = subscription.Status;
            existing.Provider = subscription.Provider;
            existing.ProviderCustomerId = subscription.ProviderCustomerId;
            existing.ProviderSubscriptionId = subscription.ProviderSubscriptionId;
            existing.ProviderCheckoutSessionId = subscription.ProviderCheckoutSessionId;
            existing.CurrentPeriodStart = subscription.CurrentPeriodStart;
            existing.CurrentPeriodEnd = subscription.CurrentPeriodEnd;
            existing.CancelAtPeriodEnd = subscription.CancelAtPeriodEnd;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
