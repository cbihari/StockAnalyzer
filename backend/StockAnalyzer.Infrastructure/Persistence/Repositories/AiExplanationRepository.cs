using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Infrastructure.Persistence.Repositories;

public sealed class AiExplanationRepository(StockAnalyzerDbContext dbContext)
    : IAiExplanationRepository
{
    public Task<AiExplanation?> FindValidAsync(
        string ticker,
        string inputHash,
        string model,
        string promptVersion,
        CancellationToken cancellationToken) =>
        dbContext.AiExplanations
            .AsNoTracking()
            .Where(value =>
                value.Ticker == ticker &&
                value.InputHash == inputHash &&
                value.Model == model &&
                value.PromptVersion == promptVersion &&
                value.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(value => value.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(AiExplanation explanation, CancellationToken cancellationToken)
    {
        dbContext.AiExplanations.Add(explanation);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

