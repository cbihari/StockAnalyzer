using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Infrastructure.Persistence.Repositories;

public sealed class StockRepository(StockAnalyzerDbContext dbContext) : IStockRepository
{
    public async Task<Stock> GetOrCreateAsync(string ticker, CancellationToken cancellationToken)
    {
        var stock = await dbContext.Stocks.SingleOrDefaultAsync(
            item => item.Ticker == ticker,
            cancellationToken);
        if (stock is not null)
        {
            return stock;
        }

        stock = new Stock { Ticker = ticker };
        dbContext.Stocks.Add(stock);
        await dbContext.SaveChangesAsync(cancellationToken);
        return stock;
    }
}
