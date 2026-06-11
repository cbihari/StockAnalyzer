using System.Text.RegularExpressions;

namespace StockAnalyzer.Domain.Stocks;

public sealed partial record StockTicker
{
    public string Value { get; }

    private StockTicker(string value) => Value = value;

    public static StockTicker Create(string value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!TickerPattern().IsMatch(normalized))
        {
            throw new ArgumentException(
                "Ticker must contain 1-20 letters, numbers, dots, carets, equals signs, or hyphens.",
                nameof(value));
        }

        return new StockTicker(normalized);
    }

    [GeneratedRegex("^[A-Z0-9.^=\\-]{1,20}$")]
    private static partial Regex TickerPattern();
}
