using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Domain.Monetization;

namespace StockAnalyzer.Infrastructure.Persistence.Repositories;

public sealed class UsageRepository(StockAnalyzerDbContext dbContext) : IUsageRepository
{
    public async Task<IReadOnlyDictionary<string, int>> GetDailyUsageAsync(
        Guid? userId,
        string clientId,
        DateOnly usageDate,
        CancellationToken cancellationToken)
    {
        var query = dbContext.UsageEvents.AsNoTracking().Where(usage =>
            usage.UsageDate == usageDate &&
            (userId.HasValue
                ? usage.UserId == userId
                : usage.UserId == null && usage.ClientId == clientId));

        var rows = await query
            .GroupBy(usage => usage.FeatureKey)
            .Select(group => new { FeatureKey = group.Key, Quantity = group.Sum(item => item.Quantity) })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.FeatureKey, row => row.Quantity, StringComparer.OrdinalIgnoreCase);
    }

    public async Task AddAsync(UsageEvent usageEvent, CancellationToken cancellationToken)
    {
        dbContext.UsageEvents.Add(usageEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
