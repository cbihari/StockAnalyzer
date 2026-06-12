using System.Text.Json.Serialization;

namespace StockAnalyzer.Application.DTOs;

public sealed record PortfolioHoldingSummaryDto(
    string Id,
    string Ticker,
    string Currency,
    double Quantity,
    [property: JsonPropertyName("average_cost")] double AverageCost,
    [property: JsonPropertyName("current_price")] double CurrentPrice,
    [property: JsonPropertyName("cost_basis")] double CostBasis,
    [property: JsonPropertyName("market_value")] double MarketValue,
    [property: JsonPropertyName("unrealized_gain")] double UnrealizedGain,
    [property: JsonPropertyName("gain_percent")] double GainPercent,
    [property: JsonPropertyName("day_change_value")] double DayChangeValue,
    [property: JsonPropertyName("weight_percent")] double WeightPercent,
    [property: JsonPropertyName("purchased_at")] string? PurchasedAt,
    string Note);

public sealed record PortfolioCurrencyBucketDto(
    string Currency,
    [property: JsonPropertyName("cost_basis")] double CostBasis,
    [property: JsonPropertyName("market_value")] double MarketValue,
    [property: JsonPropertyName("unrealized_gain")] double UnrealizedGain,
    [property: JsonPropertyName("gain_percent")] double GainPercent,
    [property: JsonPropertyName("day_change_value")] double DayChangeValue,
    [property: JsonPropertyName("holding_count")] int HoldingCount);

public sealed record PortfolioSummaryDto(
    IReadOnlyList<PortfolioHoldingSummaryDto> Holdings,
    IReadOnlyList<PortfolioCurrencyBucketDto> Buckets,
    [property: JsonPropertyName("risk_flags")] IReadOnlyList<string> RiskFlags,
    [property: JsonPropertyName("missing_tickers")] IReadOnlyList<string> MissingTickers,
    [property: JsonPropertyName("as_of")] DateTimeOffset AsOf,
    [property: JsonPropertyName("data_source")] string DataSource,
    string Disclaimer);
