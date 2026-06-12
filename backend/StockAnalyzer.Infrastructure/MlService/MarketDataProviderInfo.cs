using Microsoft.Extensions.Configuration;
using StockAnalyzer.Application.Abstractions;

namespace StockAnalyzer.Infrastructure.MlService;

public sealed class MarketDataProviderInfo(IConfiguration configuration) : IMarketDataProviderInfo
{
    public string Name => configuration["MarketDataProviderName"] ?? "yahoo_finance";
}
