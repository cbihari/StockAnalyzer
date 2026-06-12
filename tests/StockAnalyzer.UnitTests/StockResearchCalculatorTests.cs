using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Application.Services;
using Xunit;

namespace StockAnalyzer.UnitTests;

public sealed class StockResearchCalculatorTests
{
    [Fact]
    public void CalculateMarketContext_UsesRecentPriceStructure()
    {
        var history = new[]
        {
            Price("2026-06-08", 98, 103, 97, 100),
            Price("2026-06-09", 100, 106, 99, 105),
            Price("2026-06-10", 105, 109, 102, 108),
            Price("2026-06-11", 108, 112, 104, 110)
        };

        var context = StockResearchCalculator.CalculateMarketContext(history, "UP");

        Assert.Equal(97, context.Support);
        Assert.Equal(112, context.Resistance);
        Assert.Equal(4, context.LookbackSessions);
        Assert.InRange(context.RangePosition, 0.86, 0.87);
        Assert.Contains("below 97.00", context.Invalidation);
        Assert.True(context.AnnualizedVolatility > 0);
    }

    [Fact]
    public void CalculateMarketContext_DownPredictionUsesResistanceForInvalidation()
    {
        var history = new[]
        {
            Price("2026-06-10", 100, 104, 98, 102),
            Price("2026-06-11", 102, 105, 96, 99)
        };

        var context = StockResearchCalculator.CalculateMarketContext(history, "DOWN");

        Assert.Contains("above 105.00", context.Invalidation);
    }

    private static HistoricalPriceDto Price(string date, double open, double high, double low, double close) =>
        new(DateOnly.Parse(date), open, high, low, close, 1_000);
}
