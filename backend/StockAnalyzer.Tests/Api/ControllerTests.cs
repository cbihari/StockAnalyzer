using Microsoft.AspNetCore.Mvc;
using Moq;
using StockAnalyzer.Api.Controllers;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Tests.Api;

public sealed class ControllerTests
{
    [Fact]
    public void Health_ReturnsExpectedServiceStatus()
    {
        var result = Assert.IsType<OkObjectResult>(new HealthController().Get());

        Assert.Equal(200, result.StatusCode);
        Assert.Contains("stock-analyzer-api", result.Value!.ToString());
    }

    [Fact]
    public async Task Search_ForwardsQueryAndCancellationToken()
    {
        using var source = new CancellationTokenSource();
        var expected = new[]
        {
            new StockSuggestionDto("AAPL", "Apple Inc.", "NMS", "Equity", "United States")
        };
        var service = new Mock<IStockAnalysisService>();
        service.Setup(item => item.SearchStocksAsync("apple", source.Token))
            .ReturnsAsync(expected);
        var controller = new StocksController(service.Object);

        var action = await controller.Search("apple", source.Token);

        var result = Assert.IsType<OkObjectResult>(action.Result);
        Assert.Same(expected, result.Value);
        service.VerifyAll();
    }
}
