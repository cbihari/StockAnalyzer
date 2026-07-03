using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Domain.Monetization;

namespace StockAnalyzer.Infrastructure.Persistence.Repositories;

public sealed class MonetizationEventRepository(StockAnalyzerDbContext dbContext) : IMonetizationEventRepository
{
    public async Task AddAsync(MonetizationEvent monetizationEvent, CancellationToken cancellationToken)
    {
        dbContext.MonetizationEvents.Add(monetizationEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<MonetizationFunnelReportDto> GetFunnelAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var fromUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toExclusiveUtc = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var query = dbContext.MonetizationEvents.AsNoTracking().Where(value =>
            value.OccurredAt >= fromUtc &&
            value.OccurredAt < toExclusiveUtc);

        var events = await query
            .GroupBy(value => value.EventName)
            .Select(group => new MonetizationFunnelEventDto(group.Key, group.Count()))
            .OrderByDescending(value => value.Count)
            .ThenBy(value => value.EventName)
            .ToListAsync(cancellationToken);
        var breakdown = await query
            .GroupBy(value => new { value.EventName, value.Source, value.FeatureKey, value.PlanKey })
            .Select(group => new MonetizationFunnelBreakdownDto(
                group.Key.EventName,
                group.Key.Source,
                group.Key.FeatureKey,
                group.Key.PlanKey,
                group.Count()))
            .OrderByDescending(value => value.Count)
            .ThenBy(value => value.EventName)
            .ThenBy(value => value.Source)
            .ToListAsync(cancellationToken);

        return new MonetizationFunnelReportDto(
            from,
            to,
            events.Sum(value => value.Count),
            events,
            breakdown);
    }
}
