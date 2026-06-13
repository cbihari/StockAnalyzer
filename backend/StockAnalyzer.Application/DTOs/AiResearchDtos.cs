using System.Text.Json.Serialization;

namespace StockAnalyzer.Application.DTOs;

public sealed record AiResearchCitationDto(
    string Source,
    string Label,
    string Evidence,
    [property: JsonPropertyName("observed_at")] DateTimeOffset ObservedAt);

public sealed record StockResearchAnswerDto(
    string Ticker,
    string Question,
    string Answer,
    [property: JsonPropertyName("key_points")] IReadOnlyList<string> KeyPoints,
    IReadOnlyList<AiResearchCitationDto> Citations,
    IReadOnlyList<string> Limitations,
    [property: JsonPropertyName("follow_up_questions")] IReadOnlyList<string> FollowUpQuestions,
    string Disclaimer);

public sealed record MlAiResearchResponseDto(
    string Ticker,
    StockResearchAnswerDto Answer,
    string Provider,
    string Model,
    [property: JsonPropertyName("fallback_used")] bool FallbackUsed,
    [property: JsonPropertyName("fallback_reason")] string? FallbackReason,
    [property: JsonPropertyName("generated_at")] DateTimeOffset GeneratedAt,
    bool Cached,
    [property: JsonPropertyName("input_tokens")] int? InputTokens,
    [property: JsonPropertyName("output_tokens")] int? OutputTokens);

public sealed record AiResearchRequestDto(string Question);

public sealed record AiResearchResponseDto(
    string Ticker,
    StockResearchAnswerDto Answer,
    string Provider,
    string Model,
    bool FallbackUsed,
    string? FallbackReason,
    DateTimeOffset GeneratedAt,
    bool Cached);
