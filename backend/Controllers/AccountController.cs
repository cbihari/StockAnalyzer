using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockAnalyzer.Api.Auth;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/account")]
public sealed class AccountController(IGuestWorkspaceService workspaceService) : ControllerBase
{
    [HttpPost("claim-workspace")]
    public async Task<IActionResult> ClaimWorkspace(
        ClaimWorkspaceRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue) return Unauthorized();
        await workspaceService.ClaimAsync(userId.Value, request.WorkspaceId, cancellationToken);
        return NoContent();
    }
}
