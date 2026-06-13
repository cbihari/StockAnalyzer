using Microsoft.AspNetCore.Mvc;
using StockAnalyzer.Api.Auth;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Api.Controllers;

[ApiController]
[Route("api/workspace")]
public sealed class WorkspaceController(IGuestWorkspaceService workspaceService) : ControllerBase
{
    /// <summary>Returns the server-backed guest watchlist for this browser workspace.</summary>
    [HttpGet("watchlist")]
    public async Task<ActionResult<IReadOnlyList<WorkspaceWatchlistItemDto>>> GetWatchlist(
        [FromHeader(Name = "X-Client-ID")] string clientId,
        CancellationToken cancellationToken) =>
        Ok(await workspaceService.GetWatchlistAsync(User.GetUserId(), clientId, cancellationToken));

    /// <summary>Replaces the guest watchlist after validation.</summary>
    [HttpPut("watchlist")]
    public async Task<ActionResult<IReadOnlyList<WorkspaceWatchlistItemDto>>> SaveWatchlist(
        [FromHeader(Name = "X-Client-ID")] string clientId,
        [FromBody] IReadOnlyList<WorkspaceWatchlistItemDto> items,
        CancellationToken cancellationToken) =>
        Ok(await workspaceService.SaveWatchlistAsync(User.GetUserId(), clientId, items, cancellationToken));

    /// <summary>Returns alert rules and in-app notifications for this browser workspace.</summary>
    [HttpGet("alerts")]
    public async Task<ActionResult<WorkspaceAlertStateDto>> GetAlerts(
        [FromHeader(Name = "X-Client-ID")] string clientId,
        CancellationToken cancellationToken) =>
        Ok(await workspaceService.GetAlertStateAsync(User.GetUserId(), clientId, cancellationToken));

    /// <summary>Replaces guest alert rules and notifications after validation.</summary>
    [HttpPut("alerts")]
    public async Task<ActionResult<WorkspaceAlertStateDto>> SaveAlerts(
        [FromHeader(Name = "X-Client-ID")] string clientId,
        [FromBody] WorkspaceAlertStateDto state,
        CancellationToken cancellationToken) =>
        Ok(await workspaceService.SaveAlertStateAsync(User.GetUserId(), clientId, state, cancellationToken));

    /// <summary>Returns manually entered holdings for this anonymous workspace.</summary>
    [HttpGet("portfolio")]
    public async Task<ActionResult<IReadOnlyList<PortfolioHoldingDto>>> GetPortfolio(
        [FromHeader(Name = "X-Client-ID")] string clientId,
        CancellationToken cancellationToken) =>
        Ok(await workspaceService.GetPortfolioAsync(User.GetUserId(), clientId, cancellationToken));

    /// <summary>Replaces manually entered holdings after validation.</summary>
    [HttpPut("portfolio")]
    public async Task<ActionResult<IReadOnlyList<PortfolioHoldingDto>>> SavePortfolio(
        [FromHeader(Name = "X-Client-ID")] string clientId,
        [FromBody] IReadOnlyList<PortfolioHoldingDto> holdings,
        CancellationToken cancellationToken) =>
        Ok(await workspaceService.SavePortfolioAsync(User.GetUserId(), clientId, holdings, cancellationToken));
}
