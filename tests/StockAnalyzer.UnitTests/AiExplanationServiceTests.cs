using Moq;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Application.Services;
using StockAnalyzer.Domain.Stocks;
using Xunit;

namespace StockAnalyzer.UnitTests;

public sealed class AiExplanationServiceTests
{
    [Fact]
    public async Task ExplainAsync_MapsMlResponseAndPersistsMetadata()
    {
        var explanation = new StockAiExplanationDto(
            "AAPL", "UP", 72, "Grounded summary", [], [], "MEDIUM", [], [],
            "The model leans upward but is uncertain.", [],
            "Educational purpose only. Not financial advice.");
        var generatedAt = DateTimeOffset.UtcNow;
        var mlResult = new MlAiExplanationDto(
            "AAPL", explanation, "openai", "gpt-5.5", false, null, generatedAt,
            false, new string('a', 64), "ml", "stock-research-v1", 120, 45);
        var mlClient = new Mock<IMlServiceClient>();
        mlClient.Setup(value => value.GenerateAiExplanationAsync("AAPL", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mlResult);
        var repository = new Mock<IAiExplanationRepository>();
        repository.Setup(value => value.FindValidAsync(
                "AAPL", mlResult.InputHash, "gpt-5.5", "stock-research-v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AiExplanation?)null);
        AiExplanation? persisted = null;
        repository.Setup(value => value.AddAsync(It.IsAny<AiExplanation>(), It.IsAny<CancellationToken>()))
            .Callback<AiExplanation, CancellationToken>((value, _) => persisted = value)
            .Returns(Task.CompletedTask);

        var result = await new AiExplanationService(mlClient.Object, repository.Object)
            .ExplainAsync("aapl", false, CancellationToken.None);

        Assert.Equal("openai", result.Provider);
        Assert.Equal("Grounded summary", result.Explanation.Summary);
        Assert.False(result.FallbackUsed);
        Assert.Equal(120, persisted?.InputTokens);
        Assert.Equal(generatedAt.AddMinutes(15), persisted?.ExpiresAt);
    }
}
