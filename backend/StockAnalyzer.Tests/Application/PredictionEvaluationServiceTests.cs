using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Application.Exceptions;
using StockAnalyzer.Application.Services;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Tests.Application;

public sealed class PredictionEvaluationServiceTests
{
    [Fact]
    public async Task ExportHistoryCsvAsync_ChecksQuotaRecordsUsageAndReturnsCsv()
    {
        var workspaceId = Guid.NewGuid().ToString();
        var repository = new Mock<IPredictionRepository>();
        repository.Setup(value => value.GetHistoryAsync(
                null,
                workspaceId,
                "AAPL",
                "all",
                500,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new Prediction
                {
                    Stock = new Stock { Ticker = "AAPL" },
                    Ticker = "AAPL",
                    PredictionType = "ml",
                    ModelStatus = "existing_model",
                    ModelAccuracy = 0.625m,
                    PredictionValue = "UP",
                    Confidence = 72,
                    ProbabilityUp = 0.72m,
                    ProbabilityDown = 0.28m,
                    ActualResult = "DOWN",
                    IsCorrect = false,
                    CreatedAt = DateTimeOffset.Parse("2026-07-02T10:00:00Z")
                }
            ]);
        var monetization = new Mock<IMonetizationService>();
        monetization.Setup(value => value.CheckAsync(
                null,
                workspaceId,
                "csv_export",
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AllowedExport());
        monetization.Setup(value => value.RecordAsync(
                null,
                workspaceId,
                "csv_export",
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AllowedExport() with { UsedToday = 1, RemainingToday = 9 });
        var service = CreateService(repository.Object, monetization.Object);

        var result = await service.ExportHistoryCsvAsync(
            null,
            workspaceId,
            "AAPL",
            "all",
            500,
            CancellationToken.None);

        Assert.Equal("text/csv;charset=utf-8", result.ContentType);
        Assert.StartsWith("stockanalyzer-predictions-", result.FileName);
        Assert.Contains("\"ticker\",\"prediction\",\"confidence\"", result.Content);
        Assert.Contains("\"AAPL\",\"UP\",\"72\",\"0.72\",\"0.28\",\"DOWN\",\"False\"", result.Content);
        monetization.VerifyAll();
    }

    [Fact]
    public async Task ExportHistoryCsvAsync_ThrowsBeforeQuery_WhenQuotaIsExhausted()
    {
        var workspaceId = Guid.NewGuid().ToString();
        var repository = new Mock<IPredictionRepository>();
        var monetization = new Mock<IMonetizationService>();
        monetization.Setup(value => value.CheckAsync(
                null,
                workspaceId,
                "csv_export",
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UsageCheckDto(
                "csv_export",
                "CSV exports",
                "free",
                1,
                0,
                0,
                0,
                false,
                "pro",
                "This plan limit has been reached. Upgrade to Pro for higher limits."));
        var service = CreateService(repository.Object, monetization.Object);

        await Assert.ThrowsAsync<PlanLimitExceededException>(() =>
            service.ExportHistoryCsvAsync(null, workspaceId, null, "all", 500, CancellationToken.None));

        repository.Verify(value => value.GetHistoryAsync(
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
        monetization.Verify(value => value.RecordAsync(
            It.IsAny<Guid?>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static PredictionEvaluationService CreateService(
        IPredictionRepository repository,
        IMonetizationService monetization) =>
        new(
            Mock.Of<IMlServiceClient>(),
            repository,
            monetization,
            NullLogger<PredictionEvaluationService>.Instance);

    private static UsageCheckDto AllowedExport() => new(
        "csv_export",
        "CSV exports",
        "pro",
        1,
        0,
        10,
        10,
        true,
        null,
        "Feature is available for the current plan.");
}
