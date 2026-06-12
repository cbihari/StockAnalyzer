using Microsoft.AspNetCore.Mvc;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Api.Controllers;

[ApiController]
[Route("api/stocks")]
public sealed class StocksController(IStockAnalysisService stockAnalysisService) : ControllerBase
{
    /// <summary>Returns a compact batch quote snapshot for up to ten watchlist tickers.</summary>
    [HttpGet("quotes")]
    [ProducesResponseType<StockQuotesDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StockQuotesDto>> GetQuotes(
        [FromQuery] string tickers,
        CancellationToken cancellationToken = default) =>
        Ok(await stockAnalysisService.GetStockQuotesAsync(
            tickers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            cancellationToken));

    /// <summary>Returns delayed index, breadth, and liquid-universe mover snapshots.</summary>
    [HttpGet("market/overview")]
    [ProducesResponseType<MarketOverviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<MarketOverviewDto>> GetMarketOverview(
        [FromQuery] string region = "india",
        CancellationToken cancellationToken = default) =>
        Ok(await stockAnalysisService.GetMarketOverviewAsync(region, cancellationToken));

    /// <summary>Compares two or three explainable stock research snapshots.</summary>
    [HttpGet("compare")]
    [ProducesResponseType<StockComparisonDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StockComparisonDto>> Compare(
        [FromQuery] string tickers,
        [FromQuery] string period = "1y",
        CancellationToken cancellationToken = default) =>
        Ok(await stockAnalysisService.GetComparisonAsync(
            tickers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            period,
            cancellationToken));

    /// <summary>Returns a unified explainable research snapshot for a ticker.</summary>
    [HttpGet("{ticker}/analysis")]
    [ProducesResponseType<StockAnalysisDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockAnalysisDto>> GetAnalysis(
        string ticker,
        [FromQuery] string period = "1y",
        CancellationToken cancellationToken = default) =>
        Ok(await stockAnalysisService.GetAnalysisAsync(ticker, period, cancellationToken));

    /// <summary>Searches supported stocks by ticker symbol or company name.</summary>
    [HttpGet("search")]
    [ProducesResponseType<IReadOnlyList<StockSuggestionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<StockSuggestionDto>>> Search(
        [FromQuery] string query,
        CancellationToken cancellationToken) =>
        Ok(await stockAnalysisService.SearchStocksAsync(query, cancellationToken));

    /// <summary>Returns historical daily OHLCV prices for a ticker.</summary>
    [HttpGet("history/{ticker}")]
    [ProducesResponseType<IReadOnlyList<HistoricalPriceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<HistoricalPriceDto>>> GetHistory(
        string ticker,
        [FromQuery] string period = "5y",
        CancellationToken cancellationToken = default) =>
        Ok(await stockAnalysisService.GetHistoryAsync(ticker, period, cancellationToken));

    /// <summary>Returns technical indicators and the enriched price dataset.</summary>
    [HttpGet("indicators/{ticker}")]
    [ProducesResponseType<IndicatorResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IndicatorResponseDto>> GetIndicators(
        string ticker,
        [FromQuery] string period = "5y",
        CancellationToken cancellationToken = default) =>
        Ok(await stockAnalysisService.GetIndicatorsAsync(ticker, period, cancellationToken));
}
