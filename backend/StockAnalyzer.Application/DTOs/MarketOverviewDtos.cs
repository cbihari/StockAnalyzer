using System.Text.Json.Serialization;

namespace StockAnalyzer.Application.DTOs;

public sealed record MarketInstrumentDto(
    string Symbol,
    string Name,
    double Price,
    double Change,
    [property: JsonPropertyName("change_percent")] double ChangePercent,
    [property: JsonPropertyName("day_high")] double DayHigh,
    [property: JsonPropertyName("day_low")] double DayLow,
    long Volume,
    IReadOnlyList<double> Sparkline);

public sealed record MarketBreadthDto(
    int Advancers,
    int Decliners,
    int Unchanged,
    string Sentiment,
    int Coverage);

public sealed record MarketOverviewDto(
    string Region,
    [property: JsonPropertyName("session_status")] string SessionStatus,
    [property: JsonPropertyName("as_of")] DateTimeOffset AsOf,
    [property: JsonPropertyName("data_source")] string DataSource,
    [property: JsonPropertyName("coverage_note")] string CoverageNote,
    IReadOnlyList<MarketInstrumentDto> Indices,
    MarketBreadthDto Breadth,
    [property: JsonPropertyName("top_gainers")] IReadOnlyList<MarketInstrumentDto> TopGainers,
    [property: JsonPropertyName("top_losers")] IReadOnlyList<MarketInstrumentDto> TopLosers,
    [property: JsonPropertyName("most_active")] IReadOnlyList<MarketInstrumentDto> MostActive,
    IReadOnlyList<string> Insights);

public sealed record StockQuotesDto(
    [property: JsonPropertyName("as_of")] DateTimeOffset AsOf,
    [property: JsonPropertyName("data_source")] string DataSource,
    IReadOnlyList<MarketInstrumentDto> Quotes);
