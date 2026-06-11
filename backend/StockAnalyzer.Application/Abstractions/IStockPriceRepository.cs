using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Application.Abstractions;

public interface IStockPriceRepository
{
    Task UpsertRangeAsync(
        Stock stock,
        IReadOnlyCollection<StockPrice> prices,
        CancellationToken cancellationToken);
}
