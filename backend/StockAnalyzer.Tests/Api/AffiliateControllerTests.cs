using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using StockAnalyzer.Api.Auth;
using StockAnalyzer.Api.Controllers;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Tests.Api;

public sealed class AffiliateControllerTests
{
    [Fact]
    public void GetStats_RequiresAffiliateAdminPolicy()
    {
        var attribute = typeof(AffiliateController)
            .GetMethod(nameof(AffiliateController.GetStats))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.Equal(AuthPolicies.AffiliateAdmin, attribute.Policy);
    }

    [Fact]
    public async Task GetStats_ReturnsNotFound_WhenStatsAreDisabled()
    {
        var controller = CreateController(enabled: false, user: AdminUser());

        var result = await controller.GetStats(CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetStats_ReturnsForbid_WhenEnabledButUserIsNotAdmin()
    {
        var controller = CreateController(enabled: true, user: AuthenticatedUser());

        var result = await controller.GetStats(CancellationToken.None);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task GetStats_ReturnsStats_WhenEnabledAndUserHasAdminClaim()
    {
        var expected = new[]
        {
            new AffiliateClickStatDto("Zerodha", DateOnly.Parse("2026-06-30"), 3)
        };
        var repository = new Mock<IAffiliateClickRepository>();
        repository.Setup(value => value.GetStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = CreateController(enabled: true, user: AdminUser(), repository: repository);

        var result = await controller.GetStats(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
    }

    private static AffiliateController CreateController(
        bool enabled,
        ClaimsPrincipal user,
        Mock<IAffiliateClickRepository>? repository = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AffiliateAdminEnabled"] = enabled.ToString()
            })
            .Build();
        var controller = new AffiliateController(
            (repository ?? new Mock<IAffiliateClickRepository>()).Object,
            configuration);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        return controller;
    }

    private static ClaimsPrincipal AuthenticatedUser() => new(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
        "test"));

    private static ClaimsPrincipal AdminUser() => new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(AuthPolicies.AdminClaimType, "true")
        ],
        "test"));
}
