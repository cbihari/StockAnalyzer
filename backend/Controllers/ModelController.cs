using Microsoft.AspNetCore.Mvc;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Api.Controllers;

[ApiController]
[Route("api/model")]
public sealed class ModelController(
    IPredictionEvaluationService predictionEvaluationService,
    IStockAnalysisService stockAnalysisService)
    : ControllerBase
{
    /// <summary>Returns accuracy statistics for evaluated predictions.</summary>
    [HttpGet("accuracy")]
    [ProducesResponseType<PredictionAccuracyDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PredictionAccuracyDto>> GetAccuracy(
        CancellationToken cancellationToken) =>
        Ok(await predictionEvaluationService.GetAccuracyAsync(cancellationToken));

    /// <summary>Returns training metrics for a ticker-specific model.</summary>
    [HttpGet("metrics/{ticker}")]
    [ProducesResponseType<TickerModelMetricsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TickerModelMetricsDto>> GetMetrics(
        string ticker,
        CancellationToken cancellationToken) =>
        Ok(await stockAnalysisService.GetModelMetricsAsync(ticker, cancellationToken));

    /// <summary>Manually retrains and replaces the ticker-specific Random Forest model.</summary>
    [HttpPost("train/{ticker}")]
    [ProducesResponseType<ModelTrainingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<ModelTrainingDto>> Train(
        string ticker,
        [FromQuery] string period = "5y",
        CancellationToken cancellationToken = default) =>
        Ok(await stockAnalysisService.TrainModelAsync(ticker, period, cancellationToken));
}
