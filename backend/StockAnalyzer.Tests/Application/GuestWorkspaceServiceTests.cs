using Moq;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Application.Exceptions;
using StockAnalyzer.Application.Services;

namespace StockAnalyzer.Tests.Application;

public sealed class GuestWorkspaceServiceTests
{
    [Fact]
    public async Task SaveWatchlistAsync_ChecksStoredLimitAfterNormalizingTickers()
    {
        var clientId = Guid.NewGuid().ToString();
        var repository = new Mock<IGuestWorkspaceRepository>();
        repository.Setup(value => value.SaveWatchlistAsync(
                null,
                clientId,
                It.Is<string>(json => json.Contains("AAPL", StringComparison.Ordinal) && json.Contains("MSFT", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var monetization = new Mock<IMonetizationService>();
        monetization.Setup(value => value.CheckStoredLimitAsync(
                null,
                clientId,
                "watchlist_item",
                2,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Allowed("watchlist_item", "Watchlist stocks", 10));
        var service = new GuestWorkspaceService(repository.Object, monetization.Object);

        var result = await service.SaveWatchlistAsync(
            null,
            clientId,
            [
                Watchlist("aapl"),
                Watchlist("AAPL"),
                Watchlist("msft")
            ],
            CancellationToken.None);

        Assert.Equal(["AAPL", "MSFT"], result.Select(item => item.Ticker).ToArray());
        monetization.VerifyAll();
        repository.VerifyAll();
    }

    [Fact]
    public async Task SaveAlertStateAsync_ThrowsBeforePersisting_WhenAlertRuleQuotaIsExceeded()
    {
        var clientId = Guid.NewGuid().ToString();
        var repository = new Mock<IGuestWorkspaceRepository>();
        var monetization = new Mock<IMonetizationService>();
        monetization.Setup(value => value.CheckStoredLimitAsync(
                null,
                clientId,
                "alert_rule",
                6,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredLimitCheckDto(
                "alert_rule",
                "Alert rules",
                "free",
                6,
                5,
                false,
                "pro",
                "This plan limit has been reached. Upgrade to Pro for higher limits."));
        var service = new GuestWorkspaceService(repository.Object, monetization.Object);

        await Assert.ThrowsAsync<PlanLimitExceededException>(() => service.SaveAlertStateAsync(
            null,
            clientId,
            new WorkspaceAlertStateDto(
                Enumerable.Range(1, 6).Select(index => AlertRule(index)).ToArray(),
                []),
            CancellationToken.None));

        repository.Verify(value => value.SaveAlertStateAsync(
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        monetization.VerifyAll();
    }

    [Fact]
    public async Task SavePortfolioAsync_ChecksStoredLimitAfterNormalizingHoldings()
    {
        var clientId = Guid.NewGuid().ToString();
        var repository = new Mock<IGuestWorkspaceRepository>();
        repository.Setup(value => value.SavePortfolioAsync(
                null,
                clientId,
                It.Is<string>(json => json.Contains("AAPL", StringComparison.Ordinal) && json.Contains("Long term", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var monetization = new Mock<IMonetizationService>();
        monetization.Setup(value => value.CheckStoredLimitAsync(
                null,
                clientId,
                "portfolio_holding",
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Allowed("portfolio_holding", "Portfolio holdings", 10));
        var service = new GuestWorkspaceService(repository.Object, monetization.Object);

        var result = await service.SavePortfolioAsync(
            null,
            clientId,
            [Holding(" aapl ", 1.23456789, 150.123456m, "  Long term  ")],
            CancellationToken.None);

        var holding = Assert.Single(result);
        Assert.Equal("AAPL", holding.Ticker);
        Assert.Equal(1.234568, holding.Quantity);
        Assert.Equal(150.1235, holding.AverageCost);
        Assert.Equal("Long term", holding.Note);
        monetization.VerifyAll();
        repository.VerifyAll();
    }

    [Fact]
    public async Task SavePortfolioAsync_ThrowsBeforePersisting_WhenHoldingQuotaIsExceeded()
    {
        var clientId = Guid.NewGuid().ToString();
        var repository = new Mock<IGuestWorkspaceRepository>();
        var monetization = new Mock<IMonetizationService>();
        monetization.Setup(value => value.CheckStoredLimitAsync(
                null,
                clientId,
                "portfolio_holding",
                11,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StoredLimitCheckDto(
                "portfolio_holding",
                "Portfolio holdings",
                "free",
                11,
                10,
                false,
                "pro",
                "This plan limit has been reached. Upgrade to Pro for higher limits."));
        var service = new GuestWorkspaceService(repository.Object, monetization.Object);

        await Assert.ThrowsAsync<PlanLimitExceededException>(() => service.SavePortfolioAsync(
            null,
            clientId,
            new[] { "AAPL", "MSFT", "TSLA", "NVDA", "META", "AMZN", "GOOG", "NFLX", "ORCL", "IBM", "INTC" }
                .Select(ticker => Holding(ticker, 1, 100, string.Empty))
                .ToArray(),
            CancellationToken.None));

        repository.Verify(value => value.SavePortfolioAsync(
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        monetization.VerifyAll();
    }

    private static WorkspaceWatchlistItemDto Watchlist(string ticker) =>
        new(ticker, "2026-07-03T00:00:00Z", string.Empty, []);

    private static WorkspaceAlertRuleDto AlertRule(int index) =>
        new(
            $"alert-{index}",
            "AAPL",
            "price_above",
            200,
            "daily",
            24,
            string.Empty,
            string.Empty,
            true,
            "2026-07-03T00:00:00Z",
            null);

    private static PortfolioHoldingDto Holding(string ticker, double quantity, decimal averageCost, string note) =>
        new(
            Guid.NewGuid().ToString(),
            ticker,
            quantity,
            (double)averageCost,
            null,
            note);

    private static StoredLimitCheckDto Allowed(string featureKey, string label, int limit) =>
        new(
            featureKey,
            label,
            "free",
            1,
            limit,
            true,
            null,
            "Stored feature limit is available for the current plan.");
}
