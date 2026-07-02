using System.Text.Json;
using Moq;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Application.Exceptions;
using StockAnalyzer.Application.Services;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Tests.Application;

public sealed class AiExplanationServiceTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task ExplainAsync_ReturnsCachedExplanationWithoutConsumingQuota()
    {
        var cached = CachedEntity("AAPL");
        var repository = new Mock<IAiExplanationRepository>();
        repository.Setup(value => value.FindLatestValidForTickerAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);
        var mlClient = new Mock<IMlServiceClient>();
        var monetization = new Mock<IMonetizationService>();
        var service = new AiExplanationService(mlClient.Object, repository.Object, monetization.Object);

        var result = await service.ExplainAsync(
            "aapl",
            forceRefresh: false,
            userId: null,
            clientId: Guid.NewGuid().ToString(),
            CancellationToken.None);

        Assert.True(result.Cached);
        Assert.Equal("AAPL", result.Ticker);
        mlClient.Verify(
            value => value.GenerateAiExplanationAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
        monetization.Verify(
            value => value.CheckAsync(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        monetization.Verify(
            value => value.RecordAsync(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExplainAsync_ThrowsPlanLimitExceededBeforeMlCall_WhenQuotaIsExhausted()
    {
        var clientId = Guid.NewGuid().ToString();
        var repository = new Mock<IAiExplanationRepository>();
        repository.Setup(value => value.FindLatestValidForTickerAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiExplanation?)null);
        var mlClient = new Mock<IMlServiceClient>();
        var monetization = new Mock<IMonetizationService>();
        monetization.Setup(value => value.CheckAsync(null, clientId, "ai_explanation", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsageCheckDto(
                "ai_explanation",
                "AI explanations",
                "free",
                1,
                2,
                2,
                0,
                false,
                "pro",
                "This plan limit has been reached. Upgrade to Pro for higher limits."));
        var service = new AiExplanationService(mlClient.Object, repository.Object, monetization.Object);

        await Assert.ThrowsAsync<PlanLimitExceededException>(() =>
            service.ExplainAsync("AAPL", false, null, clientId, CancellationToken.None));

        mlClient.Verify(
            value => value.GenerateAiExplanationAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExplainAsync_RecordsUsage_WhenNewExplanationIsGenerated()
    {
        var clientId = Guid.NewGuid().ToString();
        var repository = new Mock<IAiExplanationRepository>();
        repository.Setup(value => value.FindLatestValidForTickerAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiExplanation?)null);
        repository.Setup(value => value.FindValidAsync(
                "AAPL",
                "hash",
                "gpt-test",
                "stock-research-v1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiExplanation?)null);
        var mlClient = new Mock<IMlServiceClient>();
        mlClient.Setup(value => value.GenerateAiExplanationAsync("AAPL", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MlResponse(cached: false));
        var monetization = new Mock<IMonetizationService>();
        monetization.Setup(value => value.CheckAsync(null, clientId, "ai_explanation", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AllowedCheck());
        monetization.Setup(value => value.RecordAsync(null, clientId, "ai_explanation", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AllowedCheck() with { UsedToday = 1, RemainingToday = 1, Message = "Usage recorded." });
        var service = new AiExplanationService(mlClient.Object, repository.Object, monetization.Object);

        var result = await service.ExplainAsync("AAPL", false, null, clientId, CancellationToken.None);

        Assert.False(result.Cached);
        repository.Verify(value => value.AddAsync(It.Is<AiExplanation>(item =>
            item.Ticker == "AAPL" &&
            item.InputHash == "hash" &&
            item.Model == "gpt-test"), It.IsAny<CancellationToken>()), Times.Once);
        monetization.Verify(value => value.RecordAsync(
            null,
            clientId,
            "ai_explanation",
            1,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExplainAsync_DoesNotRecordUsage_WhenMlReturnsCachedResponse()
    {
        var clientId = Guid.NewGuid().ToString();
        var repository = new Mock<IAiExplanationRepository>();
        repository.Setup(value => value.FindLatestValidForTickerAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiExplanation?)null);
        repository.Setup(value => value.FindValidAsync(
                "AAPL",
                "hash",
                "gpt-test",
                "stock-research-v1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiExplanation?)null);
        var mlClient = new Mock<IMlServiceClient>();
        mlClient.Setup(value => value.GenerateAiExplanationAsync("AAPL", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(MlResponse(cached: true));
        var monetization = new Mock<IMonetizationService>();
        monetization.Setup(value => value.CheckAsync(null, clientId, "ai_explanation", 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(AllowedCheck());
        var service = new AiExplanationService(mlClient.Object, repository.Object, monetization.Object);

        var result = await service.ExplainAsync("AAPL", false, null, clientId, CancellationToken.None);

        Assert.True(result.Cached);
        monetization.Verify(value => value.RecordAsync(
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static UsageCheckDto AllowedCheck() => new(
        "ai_explanation",
        "AI explanations",
        "free",
        1,
        0,
        2,
        2,
        true,
        null,
        "Feature is available for the current plan.");

    private static AiExplanation CachedEntity(string ticker) => new()
    {
        Ticker = ticker,
        InputHash = "hash",
        PredictionType = "ml",
        Provider = "openai",
        Model = "gpt-test",
        PromptVersion = "stock-research-v1",
        ExplanationJson = JsonSerializer.Serialize(Explanation(ticker), JsonOptions),
        FallbackUsed = false,
        CreatedAt = DateTimeOffset.Parse("2026-07-02T10:00:00Z"),
        ExpiresAt = DateTimeOffset.Parse("2026-07-02T10:15:00Z")
    };

    private static MlAiExplanationDto MlResponse(bool cached) => new(
        "AAPL",
        Explanation("AAPL"),
        "openai",
        "gpt-test",
        false,
        null,
        DateTimeOffset.Parse("2026-07-02T10:00:00Z"),
        cached,
        "hash",
        "ml",
        "stock-research-v1",
        100,
        200);

    private static StockAiExplanationDto Explanation(string ticker) => new(
        ticker,
        "UP",
        70,
        "Summary",
        [],
        [],
        "MEDIUM",
        [],
        ["A conflicting signal could change the view."],
        "Plain language explanation.",
        [],
        "Educational research only.");
}
