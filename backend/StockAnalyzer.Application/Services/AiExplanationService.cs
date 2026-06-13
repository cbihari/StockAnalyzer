using System.Text.Json;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Application.Services;

public sealed class AiExplanationService(
    IMlServiceClient mlServiceClient,
    IAiExplanationRepository repository) : IAiExplanationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AiExplanationResponseDto> ExplainAsync(
        string ticker,
        bool forceRefresh,
        CancellationToken cancellationToken)
    {
        var normalizedTicker = StockTicker.Create(ticker).Value;
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
                var explanation = JsonSerializer.Deserialize<StockAiExplanationDto>(
                    cached.ExplanationJson,
                    JsonOptions);
                if (explanation is not null)
                {
                    return new AiExplanationResponseDto(
                        cached.Ticker,
                        explanation,
                        cached.Provider,
                        cached.Model,
                        cached.FallbackUsed,
                        cached.FallbackReason,
                        cached.CreatedAt,
                        true);
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
        return Map(generated, generated.Cached);
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

