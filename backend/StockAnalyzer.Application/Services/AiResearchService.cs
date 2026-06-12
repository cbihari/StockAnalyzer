using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Application.Services;

public sealed class AiResearchService(IMlServiceClient mlServiceClient) : IAiResearchService
{
    public async Task<AiResearchResponseDto> AskAsync(
        string ticker,
        string question,
        CancellationToken cancellationToken)
    {
        var normalizedTicker = StockTicker.Create(ticker).Value;
        var normalizedQuestion = string.Join(' ', (question ?? string.Empty)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (normalizedQuestion.Length is < 3 or > 300)
        {
            throw new ArgumentException("Research questions must contain between 3 and 300 characters.", nameof(question));
        }

        var response = await mlServiceClient.AskAiResearchAsync(
            normalizedTicker,
            normalizedQuestion,
            cancellationToken);
        return new AiResearchResponseDto(
            response.Ticker,
            response.Answer,
            response.Provider,
            response.Model,
            response.FallbackUsed,
            response.FallbackReason,
            response.GeneratedAt,
            response.Cached);
    }
}
