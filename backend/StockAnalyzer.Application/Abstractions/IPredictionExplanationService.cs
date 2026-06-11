using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Abstractions;

public interface IPredictionExplanationService
{
    Task<PredictionExplanationDto> ExplainAsync(
        string ticker,
        CancellationToken cancellationToken);
}
