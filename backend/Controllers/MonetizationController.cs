using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StockAnalyzer.Api.Auth;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Api.Controllers;

[ApiController]
[Route("api/monetization")]
public sealed class MonetizationController(IMonetizationService monetizationService) : ControllerBase
{
    /// <summary>Returns plan metadata and current usage for the browser workspace or signed-in user.</summary>
    [HttpGet("status")]
    [ProducesResponseType<MonetizationStatusDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MonetizationStatusDto>> GetStatus(
        [FromHeader(Name = "X-Client-ID")] string clientId,
        CancellationToken cancellationToken) =>
        Ok(await monetizationService.GetStatusAsync(
            User.GetUserId(),
            clientId,
            cancellationToken));

    /// <summary>Checks whether a monetized feature is available before recording usage.</summary>
    [HttpGet("usage/check")]
    [ProducesResponseType<UsageCheckDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UsageCheckDto>> CheckUsage(
        [FromHeader(Name = "X-Client-ID")] string clientId,
        [FromQuery] string featureKey,
        [FromQuery] int quantity = 1,
        CancellationToken cancellationToken = default) =>
        Ok(await monetizationService.CheckAsync(
            User.GetUserId(),
            clientId,
            featureKey,
            quantity,
            cancellationToken));

    /// <summary>Records usage for a monetized feature when the current plan has quota.</summary>
    [HttpPost("usage")]
    [ProducesResponseType<UsageCheckDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UsageCheckDto>> RecordUsage(
        [FromHeader(Name = "X-Client-ID")] string clientId,
        [FromBody] RecordUsageRequestDto request,
        CancellationToken cancellationToken) =>
        Ok(await monetizationService.RecordAsync(
            User.GetUserId(),
            clientId,
            request.FeatureKey,
            request.Quantity,
            cancellationToken));

    /// <summary>Creates a checkout session for a paid plan. Access changes after provider confirmation.</summary>
    [Authorize]
    [HttpPost("checkout")]
    [ProducesResponseType<CheckoutResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CheckoutResponseDto>> StartCheckout(
        [FromBody] CheckoutRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new ProblemDetails { Detail = "Sign in before starting checkout." });
        }

        return Ok(await monetizationService.StartCheckoutAsync(
            userId.Value,
            request,
            cancellationToken));
    }

    /// <summary>Accepts payment provider webhooks and updates subscription state after verification.</summary>
    [HttpPost("webhooks/{provider}")]
    [ProducesResponseType<PaymentWebhookResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentWebhookResultDto>> HandleWebhook(
        string provider,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var headers = Request.Headers.ToDictionary(
            header => header.Key,
            header => header.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        return Ok(await monetizationService.HandleWebhookAsync(
            provider,
            payload,
            headers,
            cancellationToken));
    }
}
