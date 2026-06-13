using Moq;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Application.Services;
using Xunit;

namespace StockAnalyzer.UnitTests;

public sealed class AiResearchServiceTests
{
    [Fact]
    public async Task AskAsync_NormalizesRequestAndMapsGroundedResponse()
    {
        var observedAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        var generatedAt = DateTimeOffset.UtcNow;
        var answer = new StockResearchAnswerDto(
            "AAPL",
            "Why is the prediction up?",
            "The supplied evidence leans upward with uncertainty.",
            ["EMA20 is above EMA50."],
            [new AiResearchCitationDto(
                "technical_indicators", "EMA trend", "EMA20 199; EMA50 192.", observedAt)],
            [],
            ["What are the main risks?"],
            "Educational purpose only. Not financial advice.");
        var mlResponse = new MlAiResearchResponseDto(
            "AAPL", answer, "openai", "gpt-5.5", false, null, generatedAt, false, 100, 40);
        var mlClient = new Mock<IMlServiceClient>();
        mlClient.Setup(value => value.AskAiResearchAsync(
                "AAPL", "Why is the prediction up?", It.IsAny<CancellationToken>()))
            .ReturnsAsync(mlResponse);

        var result = await new AiResearchService(mlClient.Object).AskAsync(
            "aapl",
            "  Why   is the prediction up?  ",
            CancellationToken.None);

        Assert.Equal("AAPL", result.Ticker);
        Assert.Equal("openai", result.Provider);
        Assert.Single(result.Answer.Citations);
        Assert.Equal("EMA trend", result.Answer.Citations[0].Label);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no")]
    public async Task AskAsync_RejectsQuestionsThatAreTooShort(string question)
    {
        var service = new AiResearchService(Mock.Of<IMlServiceClient>());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AskAsync("AAPL", question, CancellationToken.None));
    }
}
