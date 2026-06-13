using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Services;

public sealed class PortfolioService(
    IGuestWorkspaceService workspaceService,
    IStockAnalysisService stockAnalysisService) : IPortfolioService
{
    public async Task<PortfolioSummaryDto> GetSummaryAsync(
        Guid? userId,
        string clientId,
        CancellationToken cancellationToken)
    {
        var holdings = await workspaceService.GetPortfolioAsync(userId, clientId, cancellationToken);
        if (holdings.Count == 0)
        {
            return new PortfolioSummaryDto(
                [], [], [], [], DateTimeOffset.UtcNow, "none",
                "Educational portfolio tracking only. Values are delayed and are not financial advice.");
        }

        var tickers = holdings.Select(holding => holding.Ticker).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var availableQuotes = new List<MarketInstrumentDto>();
        var failedTickers = new List<string>();
        var dataSource = "unavailable";
        var asOf = DateTimeOffset.MinValue;
        foreach (var batch in tickers.Chunk(10))
        {
            try
            {
                var response = await stockAnalysisService.GetStockQuotesAsync(batch, cancellationToken);
                availableQuotes.AddRange(response.Quotes);
                dataSource = response.DataSource;
                asOf = response.AsOf > asOf ? response.AsOf : asOf;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                failedTickers.AddRange(batch);
            }
        }

        var quoteMap = availableQuotes.ToDictionary(quote => quote.Symbol, StringComparer.OrdinalIgnoreCase);
        var missing = tickers.Where(ticker => !quoteMap.ContainsKey(ticker)).Concat(failedTickers).Distinct().ToArray();
        var draft = holdings.Where(holding => quoteMap.ContainsKey(holding.Ticker)).Select(holding =>
        {
            var quote = quoteMap[holding.Ticker];
            var currency = InferCurrency(holding.Ticker);
            var costBasis = holding.Quantity * holding.AverageCost;
            var marketValue = holding.Quantity * quote.Price;
            var gain = marketValue - costBasis;
            return new
            {
                Holding = holding, Quote = quote, Currency = currency, CostBasis = costBasis,
                MarketValue = marketValue, Gain = gain,
                GainPercent = costBasis == 0 ? 0 : gain / costBasis,
                DayChange = holding.Quantity * quote.Change
            };
        }).ToArray();

        var currencyTotals = draft.GroupBy(item => item.Currency).ToDictionary(
            group => group.Key,
            group => group.Sum(item => item.MarketValue));
        var summaries = draft.Select(item => new PortfolioHoldingSummaryDto(
            item.Holding.Id,
            item.Holding.Ticker,
            item.Currency,
            item.Holding.Quantity,
            item.Holding.AverageCost,
            item.Quote.Price,
            Round(item.CostBasis),
            Round(item.MarketValue),
            Round(item.Gain),
            item.GainPercent,
            Round(item.DayChange),
            currencyTotals[item.Currency] == 0 ? 0 : item.MarketValue / currencyTotals[item.Currency],
            item.Holding.PurchasedAt,
            item.Holding.Note))
            .OrderByDescending(item => item.MarketValue)
            .ToArray();

        var buckets = summaries.GroupBy(item => item.Currency).Select(group =>
        {
            var cost = group.Sum(item => item.CostBasis);
            var value = group.Sum(item => item.MarketValue);
            var gain = value - cost;
            return new PortfolioCurrencyBucketDto(
                group.Key, Round(cost), Round(value), Round(gain), cost == 0 ? 0 : gain / cost,
                Round(group.Sum(item => item.DayChangeValue)), group.Count());
        }).OrderBy(bucket => bucket.Currency).ToArray();

        var risks = BuildRiskFlags(summaries, buckets, missing);
        return new PortfolioSummaryDto(
            summaries, buckets, risks, missing, asOf == DateTimeOffset.MinValue ? DateTimeOffset.UtcNow : asOf, dataSource,
            "Educational portfolio tracking only. No brokerage connection, execution, tax, dividend, fee, or FX accounting is included.");
    }

    private static IReadOnlyList<string> BuildRiskFlags(
        IReadOnlyList<PortfolioHoldingSummaryDto> holdings,
        IReadOnlyList<PortfolioCurrencyBucketDto> buckets,
        IReadOnlyList<string> missing)
    {
        var flags = new List<string>();
        foreach (var holding in holdings.Where(holding => holding.WeightPercent >= .35))
            flags.Add($"{holding.Ticker} represents {holding.WeightPercent:P0} of the {holding.Currency} bucket.");
        if (holdings.Count is > 0 and < 4)
            flags.Add("Fewer than four holdings may create meaningful concentration risk.");
        if (buckets.Count > 1)
            flags.Add("Multiple currencies are shown separately because no live FX conversion is applied.");
        if (missing.Count > 0)
            flags.Add($"Quote data is missing for {string.Join(", ", missing)}.");
        return flags;
    }

    private static string InferCurrency(string ticker) =>
        ticker.EndsWith(".NS", StringComparison.OrdinalIgnoreCase) ||
        ticker.EndsWith(".BO", StringComparison.OrdinalIgnoreCase) ? "INR" : "USD";

    private static double Round(double value) => Math.Round(value, 2);
}
