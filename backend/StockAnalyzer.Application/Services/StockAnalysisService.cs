using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Application.Services;

public sealed class StockAnalysisService(
    IMlServiceClient mlServiceClient,
    IStockRepository stockRepository,
    IStockPriceRepository stockPriceRepository,
    IPredictionRepository predictionRepository,
    IModelMetricRepository modelMetricRepository) : IStockAnalysisService
{
    public async Task<IReadOnlyList<HistoricalPriceDto>> GetHistoryAsync(
        string ticker,
        string period,
        CancellationToken cancellationToken)
    {
        var normalizedTicker = StockTicker.Create(ticker).Value;
        var history = await mlServiceClient.GetHistoryAsync(normalizedTicker, period, cancellationToken);
        var stock = await stockRepository.GetOrCreateAsync(normalizedTicker, cancellationToken);
        var prices = history.Select(price => new StockPrice
        {
            StockId = stock.Id,
            Stock = stock,
            Date = price.Date,
            Open = (decimal)price.Open,
            High = (decimal)price.High,
            Low = (decimal)price.Low,
            Close = (decimal)price.Close,
            Volume = price.Volume
        }).ToArray();
        await stockPriceRepository.UpsertRangeAsync(stock, prices, cancellationToken);
        return history;
    }

    public Task<IndicatorResponseDto> GetIndicatorsAsync(
        string ticker,
        string period,
        CancellationToken cancellationToken) =>
        mlServiceClient.GetIndicatorsAsync(StockTicker.Create(ticker).Value, period, cancellationToken);

    public async Task<RuleBasedPredictionDto> GetRuleBasedPredictionAsync(
        string ticker,
        CancellationToken cancellationToken)
    {
        var normalizedTicker = StockTicker.Create(ticker).Value;
        var result = await mlServiceClient.GetRuleBasedPredictionAsync(normalizedTicker, cancellationToken);
        var stock = await stockRepository.GetOrCreateAsync(normalizedTicker, cancellationToken);
        await predictionRepository.AddAsync(new Prediction
        {
            StockId = stock.Id,
            Stock = stock,
            Ticker = normalizedTicker,
            PredictionType = "rule-based",
            PredictionValue = result.Prediction,
            Confidence = result.Confidence
        }, cancellationToken);
        return result;
    }

    public async Task<MlPredictionDto> GetMlPredictionAsync(
        string ticker,
        CancellationToken cancellationToken)
    {
        var normalizedTicker = StockTicker.Create(ticker).Value;
        var result = await mlServiceClient.GetMlPredictionAsync(normalizedTicker, cancellationToken);
        var stock = await stockRepository.GetOrCreateAsync(normalizedTicker, cancellationToken);
        await predictionRepository.AddAsync(new Prediction
        {
            StockId = stock.Id,
            Stock = stock,
            Ticker = normalizedTicker,
            PredictionType = result.PredictionType,
            ModelStatus = result.ModelStatus,
            ModelAccuracy = result.ModelAccuracy is null ? null : (decimal)result.ModelAccuracy.Value,
            PredictionValue = result.Prediction,
            Confidence = result.Confidence,
            ProbabilityUp = (decimal)result.ProbabilityUp,
            ProbabilityDown = (decimal)result.ProbabilityDown
        }, cancellationToken);
        return result;
    }

    public async Task<ModelTrainingDto> TrainModelAsync(
        string ticker,
        string period,
        CancellationToken cancellationToken)
    {
        var normalizedTicker = StockTicker.Create(ticker).Value;
        var result = await mlServiceClient.TrainModelAsync(
            normalizedTicker,
            period,
            cancellationToken);
        var stock = await stockRepository.GetOrCreateAsync(normalizedTicker, cancellationToken);
        await modelMetricRepository.AddAsync(new ModelMetric
        {
            StockId = stock.Id,
            Stock = stock,
            ModelName = "RandomForestClassifier",
            Accuracy = (decimal)result.Accuracy,
            Precision = (decimal)result.Precision,
            Recall = (decimal)result.Recall,
            ConfusionMatrix = "[]",
            FeatureImportance = "{}",
            CreatedAt = DateTimeOffset.TryParse(result.TrainedAt, out var trainedAt)
                ? trainedAt
                : DateTimeOffset.UtcNow
        }, cancellationToken);
        return result;
    }

    public Task<TickerModelMetricsDto> GetModelMetricsAsync(
        string ticker,
        CancellationToken cancellationToken) =>
        mlServiceClient.GetModelMetricsAsync(
            StockTicker.Create(ticker).Value,
            cancellationToken);
}
