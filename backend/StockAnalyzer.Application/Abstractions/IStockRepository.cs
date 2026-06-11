using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Application.Abstractions;

public interface IStockRepository
{
    Task<Stock> GetOrCreateAsync(string ticker, CancellationToken cancellationToken);
}
