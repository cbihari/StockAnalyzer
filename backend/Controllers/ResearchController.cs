using Microsoft.AspNetCore.Mvc;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Api.Controllers;

[ApiController]
[Route("api/research")]
public sealed class ResearchController(IAiResearchService aiResearchService) : ControllerBase
{
    /// <summary>Answers a ticker-scoped research question using grounded StockAnalyzer evidence.</summary>
    [HttpPost("{ticker}")]
    [ProducesResponseType<AiResearchResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<AiResearchResponseDto>> Ask(
        string ticker,
        [FromBody] AiResearchRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await aiResearchService.AskAsync(ticker, request.Question, cancellationToken));
}
