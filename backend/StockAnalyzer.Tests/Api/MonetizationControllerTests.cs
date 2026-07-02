using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using StockAnalyzer.Api.Controllers;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Tests.Api;

public sealed class MonetizationControllerTests
{
    [Fact]
    public async Task GetStatus_ForwardsWorkspaceAndUser()
    {
        var clientId = Guid.NewGuid().ToString();
        var expected = new MonetizationStatusDto("free", false, null, [], []);
        var service = new Mock<IMonetizationService>();
        service.Setup(value => value.GetStatusAsync(null, clientId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new MonetizationController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var action = await controller.GetStatus(clientId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(expected, ok.Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task RecordUsage_ForwardsRequest()
    {
        var clientId = Guid.NewGuid().ToString();
        var expected = new UsageCheckDto(
            "ai_explanation",
            "AI explanations",
            "free",
            1,
            0,
            2,
            2,
            true,
            null,
            "Usage recorded.");
        var service = new Mock<IMonetizationService>();
        service.Setup(value => value.RecordAsync(
                null,
                clientId,
                "ai_explanation",
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new MonetizationController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var action = await controller.RecordUsage(
            clientId,
            new RecordUsageRequestDto("ai_explanation"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(expected, ok.Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task StartCheckout_RequiresAuthenticatedUser()
    {
        var service = new Mock<IMonetizationService>();
        var controller = new MonetizationController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var action = await controller.StartCheckout(
            new CheckoutRequestDto("pro", "http://localhost/success", "http://localhost/cancel"),
            CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(action.Result);
    }

    [Fact]
    public async Task StartCheckout_ForwardsAuthenticatedUser()
    {
        var userId = Guid.NewGuid();
        var expected = new CheckoutResponseDto(
            "pro",
            "pending",
            "manual",
            "http://localhost/success?checkoutSession=manual_1",
            "manual_1",
            "Checkout session created.");
        var request = new CheckoutRequestDto(
            "pro",
            "http://localhost/success",
            "http://localhost/cancel");
        var service = new Mock<IMonetizationService>();
        service.Setup(value => value.StartCheckoutAsync(
                userId,
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new MonetizationController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                        "test"))
                }
            }
        };

        var action = await controller.StartCheckout(request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(expected, ok.Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task HandleWebhook_ForwardsRawPayloadAndHeaders()
    {
        const string payload = """{"status":"active","providerCheckoutSessionId":"manual_1"}""";
        var expected = new PaymentWebhookResultDto(
            "manual",
            "subscription.active",
            "pro",
            "active",
            "manual_1",
            "Subscription state updated.");
        var service = new Mock<IMonetizationService>();
        service.Setup(value => value.HandleWebhookAsync(
                "manual",
                payload,
                It.Is<IReadOnlyDictionary<string, string>>(headers => headers.ContainsKey("X-Test-Signature")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(payload))
            }
        };
        context.Request.Headers["X-Test-Signature"] = "local";
        var controller = new MonetizationController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = context
            }
        };

        var action = await controller.HandleWebhook("manual", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(expected, ok.Value);
        service.VerifyAll();
    }
}
