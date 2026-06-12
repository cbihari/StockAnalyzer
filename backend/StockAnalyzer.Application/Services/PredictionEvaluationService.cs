using Microsoft.Extensions.Logging;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Application.Exceptions;

namespace StockAnalyzer.Application.Services;

public sealed class PredictionEvaluationService(
    IMlServiceClient mlServiceClient,
    IPredictionRepository predictionRepository,
    ILogger<PredictionEvaluationService> logger) : IPredictionEvaluationService
{
    public async Task<PredictionEvaluationDto> EvaluateAsync(CancellationToken cancellationToken)
    {
        var predictions = await predictionRepository.GetUnevaluatedAsync(cancellationToken);
        var evaluatedCount = 0;

        foreach (var tickerGroup in predictions.GroupBy(prediction => prediction.Ticker))
        {
            HistoricalPriceDto[] history;
            try
            {
                history = (await mlServiceClient.GetHistoryAsync(
                        tickerGroup.Key,
                        "max",
                        cancellationToken))
                    .OrderBy(price => price.Date)
                    .ToArray();
            }
            catch (ExternalServiceException exception)
            {
                logger.LogWarning(
                    exception,
                    "Could not fetch history while evaluating {Ticker} predictions.",
                    tickerGroup.Key);
                continue;
            }

            foreach (var prediction in tickerGroup)
            {
                var predictionDate = DateOnly.FromDateTime(prediction.CreatedAt.UtcDateTime);
                var referenceIndex = Array.FindLastIndex(
                    history,
                    price => price.Date <= predictionDate);

                if (referenceIndex < 0 || referenceIndex + 1 >= history.Length)
                {
                    continue;
                }

                var referenceClose = history[referenceIndex].Close;
                var nextClose = history[referenceIndex + 1].Close;
                var actualResult = nextClose > referenceClose ? "UP" : "DOWN";

                prediction.ActualResult = actualResult;
                prediction.IsCorrect = string.Equals(
                    prediction.PredictionValue,
                    actualResult,
                    StringComparison.OrdinalIgnoreCase);
                evaluatedCount++;
            }
        }

        if (evaluatedCount > 0)
        {
            await predictionRepository.SaveChangesAsync(cancellationToken);
        }

        return new PredictionEvaluationDto(
            evaluatedCount,
            predictions.Count - evaluatedCount);
    }

    public Task<PredictionAccuracyDto> GetAccuracyAsync(CancellationToken cancellationToken) =>
        predictionRepository.GetAccuracyAsync(cancellationToken);

    public async Task<PredictionHistoryDto> GetHistoryAsync(
        string? ticker,
        string outcome,
        int limit,
        CancellationToken cancellationToken)
    {
        var normalizedOutcome = outcome.Trim().ToLowerInvariant();
        if (normalizedOutcome is not ("all" or "pending" or "correct" or "wrong"))
        {
            throw new ArgumentException("Outcome must be all, pending, correct, or wrong.", nameof(outcome));
        }

        var safeLimit = Math.Clamp(limit, 1, 500);
        var predictions = await predictionRepository.GetHistoryAsync(
            ticker,
            normalizedOutcome,
            safeLimit,
            cancellationToken);
        var evaluated = predictions.Count(prediction => prediction.IsCorrect is not null);
        var correct = predictions.Count(prediction => prediction.IsCorrect == true);
        var wrong = predictions.Count(prediction => prediction.IsCorrect == false);

        return new PredictionHistoryDto(
            predictions.Select(prediction => new PredictionHistoryItemDto(
                prediction.Id,
                prediction.Ticker,
                prediction.PredictionValue,
                (int)Math.Round(prediction.Confidence),
                prediction.ProbabilityUp is null ? null : (double)prediction.ProbabilityUp.Value,
                prediction.ProbabilityDown is null ? null : (double)prediction.ProbabilityDown.Value,
                prediction.PredictionType,
                prediction.ModelStatus,
                prediction.ModelAccuracy is null ? null : (double)prediction.ModelAccuracy.Value,
                prediction.CreatedAt,
                prediction.ActualResult,
                prediction.IsCorrect)).ToArray(),
            predictions.Count,
            evaluated,
            predictions.Count - evaluated,
            correct,
            wrong,
            evaluated == 0 ? 0 : Math.Round(correct * 100d / evaluated, 2));
    }
}
