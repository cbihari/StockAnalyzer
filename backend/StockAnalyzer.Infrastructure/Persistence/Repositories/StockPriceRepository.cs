using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Infrastructure.Persistence.Repositories;

public sealed class StockPriceRepository(StockAnalyzerDbContext dbContext) : IStockPriceRepository
{
    public async Task UpsertRangeAsync(
        Stock stock,
        IReadOnlyCollection<StockPrice> prices,
        CancellationToken cancellationToken)
    {
        if (prices.Count == 0)
        {
            return;
        }

        var dates = prices.Select(price => price.Date).ToArray();
        var existing = await dbContext.StockPrices
            .Where(price => price.StockId == stock.Id && dates.Contains(price.Date))
            .ToDictionaryAsync(price => price.Date, cancellationToken);

        foreach (var price in prices)
        {
            if (existing.TryGetValue(price.Date, out var stored))
            {
                stored.Open = price.Open;
                stored.High = price.High;
                stored.Low = price.Low;
                stored.Close = price.Close;
                stored.Volume = price.Volume;
            }
            else
            {
                dbContext.StockPrices.Add(price);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
