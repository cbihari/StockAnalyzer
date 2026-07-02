using Moq;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Application.Services;
using StockAnalyzer.Domain.Stocks;
using Xunit;

namespace StockAnalyzer.UnitTests;

public sealed class StockAnalysisServiceTests
{
    [Fact]
    public async Task GetAnalysisAsync_UsesPredictionPreviewWithoutRecordingPrediction()
    {
        var dependencies = CreateDependencies();
        var service = dependencies.CreateService();

        var analysis = await service.GetAnalysisAsync("aapl", "1y", CancellationToken.None);

        Assert.Equal("AAPL", analysis.Ticker);
        Assert.Equal("UP", analysis.Prediction.Prediction);
        dependencies.PredictionRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Prediction>(), It.IsAny<CancellationToken>()),
            Times.Never);
        dependencies.MlClient.Verify(
            client => client.GetMlPredictionAsync("AAPL", It.IsAny<CancellationToken>()),
            Times.Once);
        dependencies.MlClient.Verify(
            client => client.GetHistoryAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        dependencies.StockPriceRepository.Verify(
            repository => repository.UpsertRangeAsync(
                It.IsAny<Stock>(),
                It.Is<IReadOnlyCollection<StockPrice>>(prices => prices.Count == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMlPredictionAsync_RecordsAuditablePrediction()
    {
        var dependencies = CreateDependencies();
        var service = dependencies.CreateService();

        var prediction = await service.GetMlPredictionAsync("aapl", CancellationToken.None);

        Assert.Equal("AAPL", prediction.Ticker);
        dependencies.PredictionRepository.Verify(
            repository => repository.AddAsync(
                It.Is<Prediction>(value =>
                    value.Ticker == "AAPL" &&
                    value.PredictionType == "ml" &&
                    value.PredictionValue == "UP"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMlPredictionPreviewAsync_DoesNotRecordPrediction()
    {
        var dependencies = CreateDependencies();
        var service = dependencies.CreateService();

        var prediction = await service.GetMlPredictionPreviewAsync("aapl", CancellationToken.None);

        Assert.Equal("AAPL", prediction.Ticker);
        dependencies.PredictionRepository.Verify(
            repository => repository.AddAsync(It.IsAny<Prediction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Dependencies CreateDependencies()
    {
        var mlClient = new Mock<IMlServiceClient>();
        mlClient.Setup(client => client.GetHistoryAsync("AAPL", "1y", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                Price("2026-06-25", 100, 103, 99, 101),
                Price("2026-06-26", 101, 106, 100, 105)
            ]);
        mlClient.Setup(client => client.GetIndicatorsAsync("AAPL", "1y", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IndicatorResponseDto(
                "AAPL",
                "1y",
                new IndicatorValuesDto
                {
                    Date = DateOnly.Parse("2026-06-26"),
                    Ema20 = 104,
                    Ema50 = 100,
                    Rsi14 = 55,
                    Macd = 1.2,
                    MacdSignal = .8,
                    VolumeChange = .15
                },
                [
                    IndicatorRow("2026-06-25", 100, 103, 99, 101),
                    IndicatorRow("2026-06-26", 101, 106, 100, 105)
                ]));
        mlClient.Setup(client => client.GetMlPredictionAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MlPredictionDto(
                "AAPL",
                "UP",
                72,
                .72,
                .28,
                "RandomForestClassifier",
                105,
                ["EMA trend is positive."],
                "existing_model",
                .61,
                "Educational purpose only. Not financial advice."));

        var stock = new Stock { Id = Guid.NewGuid(), Ticker = "AAPL" };
        var stockRepository = new Mock<IStockRepository>();
        stockRepository.Setup(repository => repository.GetOrCreateAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stock);

        var stockPriceRepository = new Mock<IStockPriceRepository>();
        stockPriceRepository.Setup(repository => repository.UpsertRangeAsync(
                stock,
                It.IsAny<IReadOnlyCollection<StockPrice>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var predictionRepository = new Mock<IPredictionRepository>();
        predictionRepository.Setup(repository => repository.AddAsync(
                It.IsAny<Prediction>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var modelMetricRepository = new Mock<IModelMetricRepository>();
        var providerInfo = new Mock<IMarketDataProviderInfo>();
        providerInfo.SetupGet(info => info.Name).Returns("test_provider");

        return new Dependencies(
            mlClient,
            stockRepository,
            stockPriceRepository,
            predictionRepository,
            modelMetricRepository,
            providerInfo);
    }

    private static HistoricalPriceDto Price(string date, double open, double high, double low, double close) =>
        new(DateOnly.Parse(date), open, high, low, close, 1_000);

    private static IndicatorRowDto IndicatorRow(string date, double open, double high, double low, double close) =>
        new()
        {
            Date = DateOnly.Parse(date),
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = 1_000
        };

    private sealed record Dependencies(
        Mock<IMlServiceClient> MlClient,
        Mock<IStockRepository> StockRepository,
        Mock<IStockPriceRepository> StockPriceRepository,
        Mock<IPredictionRepository> PredictionRepository,
        Mock<IModelMetricRepository> ModelMetricRepository,
        Mock<IMarketDataProviderInfo> MarketDataProviderInfo)
    {
        public StockAnalysisService CreateService() => new(
            MlClient.Object,
            StockRepository.Object,
            StockPriceRepository.Object,
            PredictionRepository.Object,
            ModelMetricRepository.Object,
            MarketDataProviderInfo.Object);
    }
}
