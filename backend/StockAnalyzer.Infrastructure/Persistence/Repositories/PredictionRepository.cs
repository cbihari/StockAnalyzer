using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Infrastructure.Persistence.Repositories;

public sealed class PredictionRepository(StockAnalyzerDbContext dbContext) : IPredictionRepository
{
    public async Task AddAsync(Prediction prediction, CancellationToken cancellationToken)
    {
        dbContext.Predictions.Add(prediction);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Prediction>> GetUnevaluatedAsync(
        CancellationToken cancellationToken) =>
        await dbContext.Predictions
            .Where(prediction => prediction.ActualResult == null)
            .OrderBy(prediction => prediction.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<Prediction>> GetHistoryAsync(
        string? ticker,
        string outcome,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Predictions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(ticker))
        {
            var normalizedTicker = ticker.Trim().ToUpperInvariant();
            query = query.Where(prediction => prediction.Ticker == normalizedTicker);
        }

        query = outcome switch
        {
            "pending" => query.Where(prediction => prediction.IsCorrect == null),
            "correct" => query.Where(prediction => prediction.IsCorrect == true),
            "wrong" => query.Where(prediction => prediction.IsCorrect == false),
            _ => query
        };

        return await query
            .OrderByDescending(prediction => prediction.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<PredictionAccuracyDto> GetAccuracyAsync(
        CancellationToken cancellationToken)
    {
        var evaluated = dbContext.Predictions.Where(prediction => prediction.IsCorrect != null);
        var total = await evaluated.CountAsync(cancellationToken);
        var correct = await evaluated.CountAsync(prediction => prediction.IsCorrect == true, cancellationToken);
        var wrong = total - correct;
        var accuracy = total == 0 ? 0 : Math.Round(correct * 100d / total, 2);

        return new PredictionAccuracyDto(accuracy, total, correct, wrong);
    }
}
