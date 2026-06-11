using System.Text.Json.Serialization;

namespace StockAnalyzer.Application.DTOs;

public record IndicatorValuesDto
{
    public required DateOnly Date { get; init; }
    [JsonPropertyName("daily_return")] public double? DailyReturn { get; init; }
    [JsonPropertyName("SMA_20")] public double? Sma20 { get; init; }
    [JsonPropertyName("SMA_50")] public double? Sma50 { get; init; }
    [JsonPropertyName("EMA_20")] public double? Ema20 { get; init; }
    [JsonPropertyName("EMA_50")] public double? Ema50 { get; init; }
    [JsonPropertyName("RSI_14")] public double? Rsi14 { get; init; }
    [JsonPropertyName("MACD")] public double? Macd { get; init; }
    [JsonPropertyName("MACD_signal")] public double? MacdSignal { get; init; }
    [JsonPropertyName("bollinger_upper")] public double? BollingerUpper { get; init; }
    [JsonPropertyName("bollinger_lower")] public double? BollingerLower { get; init; }
    [JsonPropertyName("volume_change")] public double? VolumeChange { get; init; }
}

public sealed record IndicatorRowDto : IndicatorValuesDto
{
    public double Open { get; init; }
    public double High { get; init; }
    public double Low { get; init; }
    public double Close { get; init; }
    public long Volume { get; init; }
}

public sealed record IndicatorResponseDto(
    string Ticker,
    string Period,
    IndicatorValuesDto Latest,
    IReadOnlyList<IndicatorRowDto> Data);
