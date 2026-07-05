using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StockAnalyzer.Api.Auth;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/payments")]
public sealed class PaymentsController(IRazorpayCheckoutService razorpayCheckoutService) : ControllerBase
{
    /// <summary>Creates a Razorpay order for the signed-in user using the configured Razorpay mode.</summary>
    [HttpPost("create-order")]
    [ProducesResponseType<RazorpayOrderResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RazorpayOrderResponseDto>> CreateOrder(
        [FromBody] RazorpayOrderRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new ProblemDetails { Detail = "Sign in before starting checkout." });
        }

        return Ok(await razorpayCheckoutService.CreateOrderAsync(
            userId.Value,
            User.Identity?.Name ?? string.Empty,
            User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
            request,
            cancellationToken));
    }

    /// <summary>Verifies Razorpay Checkout payment fields before activating access.</summary>
    [HttpPost("verify")]
    [ProducesResponseType<RazorpayPaymentVerificationResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RazorpayPaymentVerificationResponseDto>> Verify(
        [FromBody] RazorpayPaymentVerificationRequestDto request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new ProblemDetails { Detail = "Sign in before verifying payment." });
        }

        return Ok(await razorpayCheckoutService.VerifyPaymentAsync(
            userId.Value,
            request,
            cancellationToken));
    }
}
