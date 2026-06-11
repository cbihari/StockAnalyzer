using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Abstractions;

public interface IPredictionEvaluationService
{
    Task<PredictionEvaluationDto> EvaluateAsync(CancellationToken cancellationToken);
    Task<PredictionAccuracyDto> GetAccuracyAsync(CancellationToken cancellationToken);
}
