using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Abstractions;

public interface IMlServiceClient
{
    Task<MarketOverviewDto> GetMarketOverviewAsync(
        string region,
        CancellationToken cancellationToken);

    Task<StockQuotesDto> GetStockQuotesAsync(
        IReadOnlyList<string> tickers,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StockSuggestionDto>> SearchStocksAsync(
        string query,
        CancellationToken cancellationToken);

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

    Task<ModelTrainingJobDto> StartTrainingJobAsync(
        string ticker,
        string period,
        CancellationToken cancellationToken);

    Task<ModelTrainingJobDto> GetTrainingJobAsync(
        string jobId,
        CancellationToken cancellationToken);

    Task<ModelVersionsDto> GetModelVersionsAsync(
        string ticker,
        CancellationToken cancellationToken);
}
