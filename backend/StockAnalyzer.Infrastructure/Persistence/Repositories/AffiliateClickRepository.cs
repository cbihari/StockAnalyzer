using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Domain.Workspace;

namespace StockAnalyzer.Infrastructure.Persistence.Repositories;

public sealed class AffiliateClickRepository(StockAnalyzerDbContext dbContext)
    : IAffiliateClickRepository
{
    public async Task AddAsync(AffiliateClick click, CancellationToken cancellationToken)
    {
        dbContext.AffiliateClicks.Add(click);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AffiliateClickStatDto>> GetStatsAsync(
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.AffiliateClicks
            .AsNoTracking()
            .GroupBy(click => new { click.Broker, Date = click.ClickedAt.Date })
            .Select(group => new { group.Key.Broker, group.Key.Date, Clicks = group.Count() })
            .OrderByDescending(stat => stat.Date)
            .ThenBy(stat => stat.Broker)
            .ToListAsync(cancellationToken);

        return rows.Select(row => new AffiliateClickStatDto(
            row.Broker,
            DateOnly.FromDateTime(row.Date),
            row.Clicks)).ToList();
    }
}
