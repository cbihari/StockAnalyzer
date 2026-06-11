namespace StockAnalyzer.Application.DTOs;

public sealed record HistoricalPriceDto(
    DateOnly Date,
    double Open,
    double High,
    double Low,
    double Close,
    long Volume);
