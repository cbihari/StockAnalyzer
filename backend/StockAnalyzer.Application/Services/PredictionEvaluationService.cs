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
}
