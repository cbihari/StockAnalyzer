using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Application.Abstractions;

public interface IModelMetricRepository
{
    Task AddAsync(ModelMetric metric, CancellationToken cancellationToken);
    Task<ModelMetric?> GetLatestAsync(string ticker, CancellationToken cancellationToken);
}
