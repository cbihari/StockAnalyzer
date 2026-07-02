using StockAnalyzer.Domain.Monetization;

namespace StockAnalyzer.Application.Abstractions;

public interface IUsageRepository
{
    Task<IReadOnlyDictionary<string, int>> GetDailyUsageAsync(
        Guid? userId,
        string clientId,
        DateOnly usageDate,
        CancellationToken cancellationToken);

    Task AddAsync(UsageEvent usageEvent, CancellationToken cancellationToken);
}
