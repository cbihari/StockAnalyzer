using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using StockAnalyzer.Api.Controllers;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Tests.Api;

public sealed class PaymentsControllerTests
{
    [Fact]
    public async Task CreateOrder_ForwardsUserEmailClaim()
    {
        var userId = Guid.NewGuid();
        var request = new RazorpayOrderRequestDto("pro");
        var expected = new RazorpayOrderResponseDto(
            "razorpay",
            "rzp_live_key",
            "order_123",
            49900,
            "INR",
            "live",
            "pro",
            "StockAnalyzer",
            "StockAnalyzer Pro plan",
            "Manual Checkout",
            "manualcheckout@stockanalyzer.local",
            "Razorpay live order created.");
        var service = new Mock<IRazorpayCheckoutService>();
        service.Setup(value => value.CreateOrderAsync(
                userId,
                "Manual Checkout",
                "manualcheckout@stockanalyzer.local",
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new PaymentsController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [
                            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                            new Claim(ClaimTypes.Name, "Manual Checkout"),
                            new Claim(ClaimTypes.Email, "manualcheckout@stockanalyzer.local")
                        ],
                        "Test"))
                }
            }
        };

        var action = await controller.CreateOrder(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(expected, ok.Value);
        service.VerifyAll();
    }
}
