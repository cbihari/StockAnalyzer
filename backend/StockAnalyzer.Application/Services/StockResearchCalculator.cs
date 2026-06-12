using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Services;

public static class StockResearchCalculator
{
    private const int DefaultLookback = 20;

    public static MarketContextDto CalculateMarketContext(
        IReadOnlyList<HistoricalPriceDto> history,
        string prediction,
        int lookbackSessions = DefaultLookback)
    {
        if (history.Count < 2)
        {
            throw new ArgumentException("At least two price rows are required.", nameof(history));
        }

        var window = history.TakeLast(Math.Min(lookbackSessions, history.Count)).ToArray();
        var latestClose = window[^1].Close;
        var support = window.Min(price => price.Low);
        var resistance = window.Max(price => price.High);
        var priceRange = resistance - support;
        var rangePosition = priceRange <= 0 ? 0.5 : Math.Clamp((latestClose - support) / priceRange, 0, 1);
        var averageDailyRange = window.Average(price =>
            price.Close == 0 ? 0 : (price.High - price.Low) / price.Close);

        var returns = new List<double>();
        for (var index = 1; index < window.Length; index++)
        {
            var previousClose = window[index - 1].Close;
            if (previousClose != 0)
            {
                returns.Add((window[index].Close - previousClose) / previousClose);
            }
        }

        var annualizedVolatility = returns.Count < 2
            ? 0
            : StandardDeviation(returns) * Math.Sqrt(252);
        var invalidation = prediction.Equals("UP", StringComparison.OrdinalIgnoreCase)
            ? $"A close below {support:F2} would weaken the current bullish interpretation."
            : $"A close above {resistance:F2} would weaken the current bearish interpretation.";

        return new MarketContextDto(
            support,
            resistance,
            rangePosition,
            annualizedVolatility,
            averageDailyRange,
            invalidation,
            window.Length);
    }

    private static double StandardDeviation(IReadOnlyList<double> values)
    {
        var average = values.Average();
        var variance = values.Sum(value => Math.Pow(value - average, 2)) / (values.Count - 1);
        return Math.Sqrt(variance);
    }
}
