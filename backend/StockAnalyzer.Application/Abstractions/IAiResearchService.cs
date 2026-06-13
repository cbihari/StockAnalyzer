using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Abstractions;

public interface IAiResearchService
{
    Task<AiResearchResponseDto> AskAsync(
        string ticker,
        string question,
        CancellationToken cancellationToken);
}
