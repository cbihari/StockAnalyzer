using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Abstractions;

public interface IAiExplanationService
{
    Task<AiExplanationResponseDto> ExplainAsync(
        string ticker,
        bool forceRefresh,
        Guid? userId,
        string clientId,
        CancellationToken cancellationToken);
}
