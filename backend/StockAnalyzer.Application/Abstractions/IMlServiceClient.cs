using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Abstractions;

public interface IMlServiceClient
{
    Task<IReadOnlyList<HistoricalPriceDto>> GetHistoryAsync(
        string ticker,
        string period,
        CancellationToken cancellationToken);

    Task<IndicatorResponseDto> GetIndicatorsAsync(
        string ticker,
        string period,
        CancellationToken cancellationToken);

    Task<RuleBasedPredictionDto> GetRuleBasedPredictionAsync(
        string ticker,
        CancellationToken cancellationToken);

    Task<MlPredictionDto> GetMlPredictionAsync(
        string ticker,
        CancellationToken cancellationToken);

    Task<ModelTrainingDto> TrainModelAsync(
        string ticker,
        string period,
        CancellationToken cancellationToken);

    Task<TickerModelMetricsDto> GetModelMetricsAsync(
        string ticker,
        CancellationToken cancellationToken);
}
