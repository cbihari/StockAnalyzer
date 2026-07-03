using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Security.Claims;
using StockAnalyzer.Api.Auth;
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
        var controller = CreateController(service.Object);

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
        var controller = CreateController(service.Object);

        var action = await controller.RecordUsage(
            clientId,
            new RecordUsageRequestDto("ai_explanation"),
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(expected, ok.Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task RecordEvent_ForwardsWorkspaceAndUser()
    {
        var clientId = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var request = new MonetizationEventRequestDto(
            "quota_callout_click",
            "portfolio",
            "portfolio_holding",
            "pro");
        var expected = new MonetizationEventResponseDto(
            "quota_callout_click",
            "Event recorded.");
        var service = new Mock<IMonetizationService>();
        service.Setup(value => value.RecordEventAsync(
                userId,
                clientId,
                request,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(service.Object, user: UserWithId(userId));

        var action = await controller.RecordEvent(clientId, request, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(action.Result);
        Assert.Same(expected, accepted.Value);
        service.VerifyAll();
    }

    [Fact]
    public async Task GetFunnel_ReturnsNotFound_WhenAdminReportsAreDisabled()
    {
        var controller = CreateController(user: AdminUser(), adminEnabled: false);

        var action = await controller.GetFunnel(30, CancellationToken.None);

        Assert.IsType<NotFoundResult>(action.Result);
    }

    [Fact]
    public async Task GetFunnel_ReturnsForbid_WhenUserIsNotAdmin()
    {
        var controller = CreateController(user: UserWithId(Guid.NewGuid()), adminEnabled: true);

        var action = await controller.GetFunnel(30, CancellationToken.None);

        Assert.IsType<ForbidResult>(action.Result);
    }

    [Fact]
    public async Task GetFunnel_ReturnsAggregateReport_WhenEnabledAndUserIsAdmin()
    {
        var expected = new MonetizationFunnelReportDto(
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-29),
            DateOnly.FromDateTime(DateTime.UtcNow),
            2,
            [new MonetizationFunnelEventDto("checkout_start", 2)],
            [new MonetizationFunnelBreakdownDto("checkout_start", "upgrade", null, "pro", 2)]);
        var repository = new Mock<IMonetizationEventRepository>();
        repository.Setup(value => value.GetFunnelAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(
            eventRepository: repository.Object,
            user: AdminUser(),
            adminEnabled: true);

        var action = await controller.GetFunnel(180, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(expected, ok.Value);
        repository.Verify(value => value.GetFunnelAsync(
            It.IsAny<DateOnly>(),
            It.IsAny<DateOnly>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportFunnel_ReturnsCsv_WhenEnabledAndUserIsAdmin()
    {
        var expected = new MonetizationFunnelReportDto(
            DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-6),
            DateOnly.FromDateTime(DateTime.UtcNow),
            3,
            [new MonetizationFunnelEventDto("quota_callout_click", 3)],
            [new MonetizationFunnelBreakdownDto("quota_callout_click", "stock_detail", "ai_explanation", null, 3)]);
        var repository = new Mock<IMonetizationEventRepository>();
        repository.Setup(value => value.GetFunnelAsync(
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(
            eventRepository: repository.Object,
            user: AdminUser(),
            adminEnabled: true);

        var action = await controller.ExportFunnel(7, CancellationToken.None);

        var file = Assert.IsType<FileContentResult>(action);
        Assert.StartsWith("text/csv", file.ContentType);
        Assert.StartsWith("stockanalyzer-monetization-funnel-", file.FileDownloadName);
        var csv = System.Text.Encoding.UTF8.GetString(file.FileContents);
        Assert.Contains("\"section\",\"event_name\",\"source\"", csv);
        Assert.Contains("\"breakdown\",\"quota_callout_click\",\"stock_detail\",\"ai_explanation\"", csv);
    }

    [Fact]
    public async Task StartCheckout_RequiresAuthenticatedUser()
    {
        var service = new Mock<IMonetizationService>();
        var controller = CreateController(service.Object);

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
        var controller = CreateController(service.Object, user: UserWithId(userId));

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
        var controller = CreateController(service.Object, httpContext: context);

        var action = await controller.HandleWebhook("manual", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(expected, ok.Value);
        service.VerifyAll();
    }

    private static MonetizationController CreateController(
        IMonetizationService? service = null,
        IMonetizationEventRepository? eventRepository = null,
        ClaimsPrincipal? user = null,
        bool adminEnabled = true,
        DefaultHttpContext? httpContext = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MonetizationAdminEnabled"] = adminEnabled.ToString()
            })
            .Build();
        httpContext ??= new DefaultHttpContext();
        if (user is not null)
        {
            httpContext.User = user;
        }

        return new MonetizationController(
            service ?? Mock.Of<IMonetizationService>(),
            eventRepository ?? Mock.Of<IMonetizationEventRepository>(),
            configuration)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };
    }

    private static ClaimsPrincipal UserWithId(Guid userId) => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
        "test"));

    private static ClaimsPrincipal AdminUser() => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(AuthPolicies.AdminClaimType, "true")
        ],
        "test"));
}
