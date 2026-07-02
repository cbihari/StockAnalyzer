using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Application.Services;

public sealed class StockAnalysisService(
    IMlServiceClient mlServiceClient,
    IStockRepository stockRepository,
    IStockPriceRepository stockPriceRepository,
    IPredictionRepository predictionRepository,
    IModelMetricRepository modelMetricRepository,
    IMarketDataProviderInfo marketDataProviderInfo) : IStockAnalysisService
{
    public Task<MarketOverviewDto> GetMarketOverviewAsync(
        string region,
        CancellationToken cancellationToken)
    {
        var normalizedRegion = region.Trim().ToLowerInvariant();
        if (normalizedRegion is not ("india" or "us"))
        {
            throw new ArgumentException("Region must be india or us.", nameof(region));
        }
        return mlServiceClient.GetMarketOverviewAsync(normalizedRegion, cancellationToken);
    }

    public Task<StockQuotesDto> GetStockQuotesAsync(
        IReadOnlyList<string> tickers,
        CancellationToken cancellationToken)
    {
        var normalized = tickers
            .Where(ticker => !string.IsNullOrWhiteSpace(ticker))
            .Select(ticker => StockTicker.Create(ticker).Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (normalized.Length is < 1 or > 10)
        {
            throw new ArgumentException("Provide between 1 and 10 unique tickers.", nameof(tickers));
        }
        return mlServiceClient.GetStockQuotesAsync(normalized, cancellationToken);
    }

    public Task<StockNewsDto> GetStockNewsAsync(
        string ticker,
        int lookbackDays,
        int limit,
        CancellationToken cancellationToken)
    {
        if (lookbackDays is < 1 or > 30)
        {
            throw new ArgumentException("News lookback must be between 1 and 30 days.", nameof(lookbackDays));
        }
        if (limit is < 1 or > 20)
        {
            throw new ArgumentException("News limit must be between 1 and 20 articles.", nameof(limit));
        }
        return mlServiceClient.GetStockNewsAsync(
            StockTicker.Create(ticker).Value,
            lookbackDays,
            limit,
            cancellationToken);
    }

    public async Task<StockComparisonDto> GetComparisonAsync(
        IReadOnlyList<string> tickers,
        string period,
        CancellationToken cancellationToken)
    {
        var normalizedTickers = tickers
            .Where(ticker => !string.IsNullOrWhiteSpace(ticker))
            .Select(ticker => StockTicker.Create(ticker).Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedTickers.Length is < 2 or > 3)
        {
            throw new ArgumentException("Comparison requires 2 or 3 unique tickers.", nameof(tickers));
        }

        var analyses = new List<StockAnalysisDto>(normalizedTickers.Length);
        foreach (var ticker in normalizedTickers)
        {
            analyses.Add(await GetAnalysisAsync(ticker, period, cancellationToken));
        }

        return new StockComparisonDto(
            period,
            analyses,
            DateTimeOffset.UtcNow,
            "Comparison is educational research only. It does not account for portfolio suitability or constitute financial advice.");
    }

    public async Task<StockAnalysisDto> GetAnalysisAsync(
        string ticker,
        string period,
        CancellationToken cancellationToken)
    {
        var normalizedTicker = StockTicker.Create(ticker).Value;
        var indicators = await GetIndicatorsAsync(normalizedTicker, period, cancellationToken);
        var history = indicators.Data.Select(ToHistoricalPrice).ToArray();
        await PersistHistoryAsync(normalizedTicker, history, cancellationToken);
        var prediction = await GetMlPredictionPreviewAsync(normalizedTicker, cancellationToken);

        if (history.Length < 2)
        {
            throw new ArgumentException("At least two price rows are required for analysis.", nameof(ticker));
        }

        var latest = history[^1];
        var previous = history[^2];
        var dailyChange = latest.Close - previous.Close;
        var dailyChangePercent = previous.Close == 0 ? 0 : dailyChange / previous.Close;
        var supporting = new List<AnalysisSignalDto>();
        var conflicting = new List<AnalysisSignalDto>();
        BuildSignals(indicators.Latest, prediction.Prediction, supporting, conflicting);
        var marketContext = StockResearchCalculator.CalculateMarketContext(history, prediction.Prediction);
        var risk = BuildRisk(indicators.Latest, prediction, supporting.Count, conflicting.Count, marketContext);

        return new StockAnalysisDto(
            normalizedTicker,
            new StockQuoteDto(
                normalizedTicker,
                latest.Close,
                previous.Close,
                dailyChange,
                dailyChangePercent,
                latest.Date,
                InferCurrency(normalizedTicker)),
            prediction,
            indicators,
            history,
            supporting,
            conflicting,
            risk,
            marketContext,
            DescribeTrend(indicators.Latest),
            DescribeVolume(indicators.Latest.VolumeChange),
            marketDataProviderInfo.Name,
            DateTimeOffset.UtcNow,
            "Educational research only. Not financial advice or a recommendation to buy or sell.");
    }

    public Task<IReadOnlyList<StockSuggestionDto>> SearchStocksAsync(
        string query,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = query?.Trim() ?? string.Empty;
        if (normalizedQuery.Length < 2)
        {
            throw new ArgumentException("Search query must contain at least 2 characters.", nameof(query));
        }
        return mlServiceClient.SearchStocksAsync(normalizedQuery, cancellationToken);
    }

    public async Task<IReadOnlyList<HistoricalPriceDto>> GetHistoryAsync(
        string ticker,
        string period,
        CancellationToken cancellationToken)
    {
        var normalizedTicker = StockTicker.Create(ticker).Value;
        var history = await mlServiceClient.GetHistoryAsync(normalizedTicker, period, cancellationToken);
        await PersistHistoryAsync(normalizedTicker, history, cancellationToken);
        return history;
    }

    private async Task PersistHistoryAsync(
        string normalizedTicker,
        IReadOnlyList<HistoricalPriceDto> history,
        CancellationToken cancellationToken)
    {
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
    }

    private static HistoricalPriceDto ToHistoricalPrice(IndicatorRowDto row) =>
        new(row.Date, row.Open, row.High, row.Low, row.Close, row.Volume);

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
        var result = await GetMlPredictionPreviewAsync(normalizedTicker, cancellationToken);
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

    public Task<MlPredictionDto> GetMlPredictionPreviewAsync(
        string ticker,
        CancellationToken cancellationToken) =>
        mlServiceClient.GetMlPredictionAsync(StockTicker.Create(ticker).Value, cancellationToken);

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

    public Task<ModelTrainingJobDto> StartTrainingJobAsync(
        string ticker,
        string period,
        CancellationToken cancellationToken) =>
        mlServiceClient.StartTrainingJobAsync(
            StockTicker.Create(ticker).Value,
            period,
            cancellationToken);

    public Task<ModelTrainingJobDto> GetTrainingJobAsync(
        string jobId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(jobId, out _))
        {
            throw new ArgumentException("Training job ID is invalid.", nameof(jobId));
        }
        return mlServiceClient.GetTrainingJobAsync(jobId, cancellationToken);
    }

    public Task<ModelVersionsDto> GetModelVersionsAsync(
        string ticker,
        CancellationToken cancellationToken) =>
        mlServiceClient.GetModelVersionsAsync(
            StockTicker.Create(ticker).Value,
            cancellationToken);

    private static void BuildSignals(
        IndicatorValuesDto values,
        string prediction,
        ICollection<AnalysisSignalDto> supporting,
        ICollection<AnalysisSignalDto> conflicting)
    {
        AddSignal(values.Ema20, values.Ema50, "Trend", "EMA alignment",
            "EMA20 is above EMA50, supporting a bullish trend.",
            "EMA20 is at or below EMA50, supporting a bearish trend.", prediction, supporting, conflicting);
        AddSignal(values.Macd, values.MacdSignal, "Momentum", "MACD",
            "MACD is above its signal line, showing positive momentum.",
            "MACD is at or below its signal line, showing weak momentum.", prediction, supporting, conflicting);

        if (values.Rsi14 is { } rsi)
        {
            var direction = rsi < 35 ? "UP" : rsi > 70 ? "DOWN" : "NEUTRAL";
            var detail = direction switch
            {
                "UP" => $"RSI {rsi:F1} is in an oversold area where momentum can recover.",
                "DOWN" => $"RSI {rsi:F1} is in an overbought area where momentum can cool.",
                _ => $"RSI {rsi:F1} is neutral and does not strongly favor either direction."
            };
            AddCategorized(new AnalysisSignalDto("Momentum", "RSI 14", detail, direction), prediction, supporting, conflicting);
        }

        if (values.VolumeChange is { } volumeChange)
        {
            var direction = volumeChange > 0 ? prediction : "NEUTRAL";
            var detail = volumeChange > 0
                ? $"Volume increased {volumeChange:P1}, adding participation to the current signal."
                : $"Volume changed {volumeChange:P1} and does not confirm the current signal.";
            AddCategorized(new AnalysisSignalDto("Participation", "Volume", detail, direction), prediction, supporting, conflicting);
        }
    }

    private static void AddSignal(
        double? left,
        double? right,
        string category,
        string label,
        string bullishDetail,
        string bearishDetail,
        string prediction,
        ICollection<AnalysisSignalDto> supporting,
        ICollection<AnalysisSignalDto> conflicting)
    {
        if (left is null || right is null) return;
        AddCategorized(
            new AnalysisSignalDto(category, label, left > right ? bullishDetail : bearishDetail, left > right ? "UP" : "DOWN"),
            prediction,
            supporting,
            conflicting);
    }

    private static void AddCategorized(
        AnalysisSignalDto signal,
        string prediction,
        ICollection<AnalysisSignalDto> supporting,
        ICollection<AnalysisSignalDto> conflicting)
    {
        if (signal.Direction == "NEUTRAL") conflicting.Add(signal);
        else if (signal.Direction == prediction) supporting.Add(signal);
        else conflicting.Add(signal);
    }

    private static RiskAssessmentDto BuildRisk(
        IndicatorValuesDto values,
        MlPredictionDto prediction,
        int supportingCount,
        int conflictingCount,
        MarketContextDto marketContext)
    {
        var score = 20;
        var factors = new List<string>();
        if (prediction.Confidence < 60) { score += 25; factors.Add("The directional estimate has low confidence."); }
        else if (prediction.Confidence < 70) { score += 12; factors.Add("The directional estimate has moderate confidence."); }
        if (conflictingCount >= supportingCount) { score += 22; factors.Add("Technical signals are mixed or conflicting."); }
        if (prediction.FallbackUsed) { score += 25; factors.Add("A rule-based fallback is being used instead of a trained model."); }
        if (values.DailyReturn is { } dailyReturn && Math.Abs(dailyReturn) > 0.03) { score += 18; factors.Add("The latest daily move indicates elevated volatility."); }
        if (values.VolumeChange is { } volume && Math.Abs(volume) > 1) { score += 10; factors.Add("Volume changed unusually compared with the prior session."); }
        if (marketContext.AnnualizedVolatility > 0.45) { score += 18; factors.Add("Recent annualized volatility is elevated."); }
        else if (marketContext.AnnualizedVolatility > 0.3) { score += 10; factors.Add("Recent price volatility is above a moderate level."); }
        score = Math.Min(score, 100);
        var level = score >= 70 ? "HIGH" : score >= 40 ? "MEDIUM" : "LOW";
        if (factors.Count == 0) factors.Add("The available signals are relatively aligned, but market risk remains.");
        return new RiskAssessmentDto(level, score, factors,
            level == "HIGH" ? "Evidence is uncertain or market conditions are unstable."
            : level == "MEDIUM" ? "The signal has useful evidence, but meaningful uncertainty remains."
            : "Signals are relatively aligned; this does not remove market risk.");
    }

    private static string DescribeTrend(IndicatorValuesDto values) =>
        values.Ema20 is null || values.Ema50 is null ? "DATA_LIMITED"
        : values.Ema20 > values.Ema50 ? "BULLISH" : "BEARISH";

    private static string DescribeVolume(double? volumeChange) =>
        volumeChange is null ? "DATA_LIMITED"
        : volumeChange > 0.2 ? "EXPANDING"
        : volumeChange < -0.2 ? "CONTRACTING" : "STABLE";

    private static string InferCurrency(string ticker) =>
        ticker.EndsWith(".NS", StringComparison.OrdinalIgnoreCase) ||
        ticker.EndsWith(".BO", StringComparison.OrdinalIgnoreCase) ? "INR" : "USD";
}
