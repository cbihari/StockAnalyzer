using Microsoft.AspNetCore.Mvc;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Api.Controllers;

[ApiController]
[Route("api/stocks")]
public sealed class StocksController(IStockAnalysisService stockAnalysisService) : ControllerBase
{
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
