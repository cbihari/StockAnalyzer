using StockAnalyzer.Domain.Stocks;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Abstractions;

public interface IPredictionRepository
{
    Task AddAsync(Prediction prediction, CancellationToken cancellationToken);
    Task<IReadOnlyList<Prediction>> GetUnevaluatedAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Prediction>> GetHistoryAsync(
        string? ticker,
        string outcome,
        int limit,
        CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<PredictionAccuracyDto> GetAccuracyAsync(CancellationToken cancellationToken);
}
