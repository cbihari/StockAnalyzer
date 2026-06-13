using System.Text.Json.Serialization;

namespace StockAnalyzer.Application.DTOs;

public sealed record AiExplanationSignalDto(
    string Signal,
    string Explanation,
    string Importance);

public sealed record StockAiExplanationDto(
    string Ticker,
    string Prediction,
    int Confidence,
    string Summary,
    [property: JsonPropertyName("supporting_signals")] IReadOnlyList<AiExplanationSignalDto> SupportingSignals,
    [property: JsonPropertyName("conflicting_signals")] IReadOnlyList<AiExplanationSignalDto> ConflictingSignals,
    [property: JsonPropertyName("risk_level")] string RiskLevel,
    [property: JsonPropertyName("risk_factors")] IReadOnlyList<string> RiskFactors,
    [property: JsonPropertyName("what_could_change_the_view")] IReadOnlyList<string> WhatCouldChangeTheView,
    [property: JsonPropertyName("beginner_explanation")] string BeginnerExplanation,
    [property: JsonPropertyName("data_limitations")] IReadOnlyList<string> DataLimitations,
    string Disclaimer);

public sealed record MlAiExplanationDto(
    string Ticker,
    StockAiExplanationDto Explanation,
    string Provider,
    string Model,
    [property: JsonPropertyName("fallback_used")] bool FallbackUsed,
    [property: JsonPropertyName("fallback_reason")] string? FallbackReason,
    [property: JsonPropertyName("generated_at")] DateTimeOffset GeneratedAt,
    bool Cached,
    [property: JsonPropertyName("input_hash")] string InputHash,
    [property: JsonPropertyName("prediction_type")] string PredictionType,
    [property: JsonPropertyName("prompt_version")] string PromptVersion,
    [property: JsonPropertyName("input_tokens")] int? InputTokens,
    [property: JsonPropertyName("output_tokens")] int? OutputTokens);

public sealed record AiExplanationResponseDto(
    string Ticker,
    StockAiExplanationDto Explanation,
    string Provider,
    string Model,
    bool FallbackUsed,
    string? FallbackReason,
    DateTimeOffset GeneratedAt,
    bool Cached);

