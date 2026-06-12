namespace StockAnalyzer.Application.DTOs;

public sealed record HistoricalPriceDto(
    DateOnly Date,
    double Open,
    double High,
    double Low,
    double Close,
    long Volume);

public sealed record StockSuggestionDto(
    string Symbol,
    string Name,
    string Exchange,
    string Type,
    string Country);
