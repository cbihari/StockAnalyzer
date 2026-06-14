namespace StockAnalyzer.Application.DTOs;

public sealed record AffiliatePartnerDto(string Name, string Url, string Logo);

public sealed record AffiliateClickRequestDto(string Broker, string? Ticker);

public sealed record AffiliateClickStatDto(string Broker, DateOnly Date, int Clicks);
