using Microsoft.AspNetCore.Mvc;
using StockAnalyzer.Api.Auth;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Api.Controllers;

[ApiController]
[Route("api/portfolio")]
public sealed class PortfolioController(IPortfolioService portfolioService) : ControllerBase
{
    /// <summary>Returns delayed valuation, performance, allocation, and concentration context for manually entered holdings.</summary>
    [HttpGet("summary")]
    [ProducesResponseType<PortfolioSummaryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PortfolioSummaryDto>> GetSummary(
        [FromHeader(Name = "X-Client-ID")] string clientId,
        CancellationToken cancellationToken) =>
        Ok(await portfolioService.GetSummaryAsync(User.GetUserId(), clientId, cancellationToken));
}
