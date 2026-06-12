using Microsoft.AspNetCore.Mvc;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Api.Controllers;

[ApiController]
[Route("api/predictions")]
public sealed class PredictionsController(
    IStockAnalysisService stockAnalysisService,
    IPredictionEvaluationService predictionEvaluationService,
    IPredictionExplanationService predictionExplanationService) : ControllerBase
{
    /// <summary>Creates a deterministic rule-based UP or DOWN prediction.</summary>
    [HttpGet("rule-based/{ticker}")]
    [ProducesResponseType<RuleBasedPredictionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RuleBasedPredictionDto>> GetRuleBased(
        string ticker,
        CancellationToken cancellationToken) =>
        Ok(await stockAnalysisService.GetRuleBasedPredictionAsync(ticker, cancellationToken));

    /// <summary>Loads or trains a ticker-specific model and returns its prediction or rule fallback.</summary>
    [HttpGet("ml/{ticker}")]
    [ProducesResponseType<MlPredictionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<MlPredictionDto>> GetMl(
        string ticker,
        CancellationToken cancellationToken) =>
        Ok(await stockAnalysisService.GetMlPredictionAsync(ticker, cancellationToken));

    /// <summary>Evaluates pending predictions with the next available trading-day close.</summary>
    [HttpPost("evaluate")]
    [ProducesResponseType<PredictionEvaluationDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PredictionEvaluationDto>> Evaluate(
        CancellationToken cancellationToken) =>
        Ok(await predictionEvaluationService.EvaluateAsync(cancellationToken));

    /// <summary>Returns the persisted prediction audit trail with optional ticker and outcome filters.</summary>
    [HttpGet("history")]
    [ProducesResponseType<PredictionHistoryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PredictionHistoryDto>> GetHistory(
        [FromQuery] string? ticker = null,
        [FromQuery] string outcome = "all",
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default) =>
        Ok(await predictionEvaluationService.GetHistoryAsync(
            ticker,
            outcome,
            limit,
            cancellationToken));

    /// <summary>Explains an ML prediction using deterministic technical-indicator rules.</summary>
    [HttpGet("explain/{ticker}")]
    [ProducesResponseType<PredictionExplanationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PredictionExplanationDto>> Explain(
        string ticker,
        CancellationToken cancellationToken) =>
        Ok(await predictionExplanationService.ExplainAsync(ticker, cancellationToken));
}
