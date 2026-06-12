using System.Text.Json.Serialization;

namespace StockAnalyzer.Application.DTOs;

public sealed record StockQuoteDto(
    string Ticker,
    double LatestPrice,
    double PreviousClose,
    [property: JsonPropertyName("daily_change")] double DailyChange,
    [property: JsonPropertyName("daily_change_percent")] double DailyChangePercent,
    [property: JsonPropertyName("as_of")] DateOnly AsOf,
    string Currency);

public sealed record AnalysisSignalDto(
    string Category,
    string Label,
    string Detail,
    string Direction);

public sealed record RiskAssessmentDto(
    string Level,
    int Score,
    IReadOnlyList<string> Factors,
    string Summary);

public sealed record MarketContextDto(
    double Support,
    double Resistance,
    double RangePosition,
    double AnnualizedVolatility,
    double AverageDailyRange,
    string Invalidation,
    int LookbackSessions);

public sealed record StockAnalysisDto(
    string Ticker,
    StockQuoteDto Quote,
    MlPredictionDto Prediction,
    IndicatorResponseDto Indicators,
    IReadOnlyList<HistoricalPriceDto> History,
    IReadOnlyList<AnalysisSignalDto> SupportingSignals,
    IReadOnlyList<AnalysisSignalDto> ConflictingSignals,
    RiskAssessmentDto Risk,
    MarketContextDto MarketContext,
    string Trend,
    [property: JsonPropertyName("volume_state")] string VolumeState,
    [property: JsonPropertyName("data_source")] string DataSource,
    [property: JsonPropertyName("generated_at")] DateTimeOffset GeneratedAt,
    string Disclaimer);

public sealed record StockComparisonDto(
    string Period,
    IReadOnlyList<StockAnalysisDto> Stocks,
    [property: JsonPropertyName("generated_at")] DateTimeOffset GeneratedAt,
    string Disclaimer);
