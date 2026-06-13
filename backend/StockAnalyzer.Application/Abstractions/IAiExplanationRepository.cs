using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Application.Abstractions;

public interface IAiExplanationRepository
{
    Task<AiExplanation?> FindValidAsync(
        string ticker,
        string inputHash,
        string model,
        string promptVersion,
        CancellationToken cancellationToken);

    Task AddAsync(AiExplanation explanation, CancellationToken cancellationToken);
}

