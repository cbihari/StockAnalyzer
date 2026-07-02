using System.Text.Json;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Application.Exceptions;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Application.Services;

public sealed class AiExplanationService(
    IMlServiceClient mlServiceClient,
    IAiExplanationRepository repository,
    IMonetizationService monetizationService) : IAiExplanationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string AiExplanationFeature = "ai_explanation";

    public async Task<AiExplanationResponseDto> ExplainAsync(
        string ticker,
        bool forceRefresh,
        Guid? userId,
        string clientId,
        CancellationToken cancellationToken)
    {
        var normalizedTicker = StockTicker.Create(ticker).Value;
        if (!forceRefresh)
        {
            var latestCached = await repository.FindLatestValidForTickerAsync(
                normalizedTicker,
                cancellationToken);
            if (latestCached is not null && TryMapCached(latestCached) is { } cachedResponse)
            {
                return cachedResponse;
            }
        }

        var quota = await monetizationService.CheckAsync(
            userId,
            clientId,
            AiExplanationFeature,
            1,
            cancellationToken);
        if (!quota.Allowed)
        {
            throw new PlanLimitExceededException(quota.Message);
        }

        var generated = await mlServiceClient.GenerateAiExplanationAsync(
            normalizedTicker,
            forceRefresh,
            cancellationToken);

        if (!forceRefresh)
        {
            var cached = await repository.FindValidAsync(
                normalizedTicker,
                generated.InputHash,
                generated.Model,
                generated.PromptVersion,
                cancellationToken);
            if (cached is not null)
            {
                if (TryMapCached(cached) is { } cachedResponse)
                {
                    return cachedResponse;
                }
            }
        }

        var entity = new AiExplanation
        {
            Ticker = normalizedTicker,
            InputHash = generated.InputHash,
            PredictionType = generated.PredictionType,
            Provider = generated.Provider,
            Model = generated.Model,
            PromptVersion = generated.PromptVersion,
            ExplanationJson = JsonSerializer.Serialize(generated.Explanation, JsonOptions),
            InputTokens = generated.InputTokens,
            OutputTokens = generated.OutputTokens,
            FallbackUsed = generated.FallbackUsed,
            FallbackReason = generated.FallbackReason,
            CreatedAt = generated.GeneratedAt,
            ExpiresAt = generated.GeneratedAt.AddMinutes(15)
        };
        await repository.AddAsync(entity, cancellationToken);
        if (!generated.Cached)
        {
            await monetizationService.RecordAsync(
                userId,
                clientId,
                AiExplanationFeature,
                1,
                cancellationToken);
        }
        return Map(generated, generated.Cached);
    }

    private static AiExplanationResponseDto? TryMapCached(AiExplanation cached)
    {
        var explanation = JsonSerializer.Deserialize<StockAiExplanationDto>(
            cached.ExplanationJson,
            JsonOptions);
        return explanation is null
            ? null
            : new AiExplanationResponseDto(
                cached.Ticker,
                explanation,
                cached.Provider,
                cached.Model,
                cached.FallbackUsed,
                cached.FallbackReason,
                cached.CreatedAt,
                true);
    }

    private static AiExplanationResponseDto Map(MlAiExplanationDto value, bool cached) => new(
        value.Ticker,
        value.Explanation,
        value.Provider,
        value.Model,
        value.FallbackUsed,
        value.FallbackReason,
        value.GeneratedAt,
        cached);
}
