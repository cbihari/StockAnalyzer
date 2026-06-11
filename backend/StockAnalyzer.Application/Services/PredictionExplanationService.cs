using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Application.Services;

public sealed class PredictionExplanationService(IMlServiceClient mlServiceClient)
    : IPredictionExplanationService
{
    private const string RiskWarning =
        "This is an educational, model-based estimate, not financial advice. " +
        "Markets can move unexpectedly, so do not make investment decisions from this prediction alone.";

    public async Task<PredictionExplanationDto> ExplainAsync(
        string ticker,
        CancellationToken cancellationToken)
    {
        var normalizedTicker = StockTicker.Create(ticker).Value;
        var predictionTask = mlServiceClient.GetMlPredictionAsync(
            normalizedTicker,
            cancellationToken);
        var indicatorsTask = mlServiceClient.GetIndicatorsAsync(
            normalizedTicker,
            "1y",
            cancellationToken);

        await Task.WhenAll(predictionTask, indicatorsTask);

        var prediction = await predictionTask;
        var latest = (await indicatorsTask).Latest;
        var reasons = BuildTechnicalReasons(latest);
        var direction = prediction.Prediction.Equals("UP", StringComparison.OrdinalIgnoreCase)
            ? "rise"
            : "fall";

        return new PredictionExplanationDto(
            normalizedTicker,
            prediction.Prediction,
            prediction.Confidence,
            reasons,
            RiskWarning,
            $"The model estimates that {normalizedTicker} may {direction} on the next trading day " +
            $"with {prediction.Confidence}% confidence. The technical reasons show which recent " +
            "price and trading-volume patterns support or disagree with that estimate.");
    }

    private static IReadOnlyList<string> BuildTechnicalReasons(IndicatorValuesDto indicators)
    {
        var reasons = new List<string>();

        if (indicators.Rsi14 is double rsi)
        {
            reasons.Add(rsi switch
            {
                < 35 => $"RSI is {rsi:F1}, which suggests the stock may be oversold and can support an upward move.",
                > 70 => $"RSI is {rsi:F1}, which suggests the stock may be overbought and can increase downside risk.",
                _ => $"RSI is {rsi:F1}, which is in a neutral range and does not strongly favor either direction."
            });
        }

        if (indicators.Ema20 is double ema20 && indicators.Ema50 is double ema50)
        {
            reasons.Add(ema20 > ema50
                ? "EMA 20 is above EMA 50, indicating positive short-term price momentum."
                : "EMA 20 is below EMA 50, indicating negative short-term price momentum.");
        }

        if (indicators.Macd is double macd && indicators.MacdSignal is double signal)
        {
            reasons.Add(macd > signal
                ? "MACD is above its signal line, which supports bullish momentum."
                : "MACD is below its signal line, which supports bearish momentum.");
        }

        if (indicators.VolumeChange is double volumeChange)
        {
            reasons.Add(volumeChange > 0
                ? $"Trading volume increased by {volumeChange:P1}, adding support to the current move."
                : $"Trading volume changed by {volumeChange:P1}, so volume does not strongly confirm the current move.");
        }

        return reasons.Count > 0
            ? reasons
            : ["Not enough indicator data is available to provide technical reasons."];
    }
}
