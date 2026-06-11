using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Infrastructure.Persistence.Repositories;

public sealed class ModelMetricRepository(StockAnalyzerDbContext dbContext) : IModelMetricRepository
{
    public async Task AddAsync(ModelMetric metric, CancellationToken cancellationToken)
    {
        dbContext.ModelMetrics.Add(metric);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<ModelMetric?> GetLatestAsync(string ticker, CancellationToken cancellationToken) =>
        dbContext.ModelMetrics
            .AsNoTracking()
            .Where(metric => metric.Stock.Ticker == ticker)
            .OrderByDescending(metric => metric.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
}
