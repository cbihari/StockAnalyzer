using StockAnalyzer.Domain.Stocks;
using Xunit;

namespace StockAnalyzer.UnitTests;

public sealed class StockTickerTests
{
    [Theory]
    [InlineData(" reliance.ns ", "RELIANCE.NS")]
    [InlineData("^nsei", "^NSEI")]
    [InlineData("btc-usd", "BTC-USD")]
    public void Create_NormalizesValidTicker(string input, string expected)
    {
        var ticker = StockTicker.Create(input);

        Assert.Equal(expected, ticker.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AAPL/USD")]
    [InlineData("SYMBOL THAT IS TOO LONG")]
    public void Create_RejectsInvalidTicker(string input)
    {
        var exception = Assert.Throws<ArgumentException>(() => StockTicker.Create(input));

        Assert.Contains("Ticker must contain", exception.Message);
    }
}
