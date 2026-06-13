namespace StockAnalyzer.Domain.Stocks;

public sealed class AiExplanation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Ticker { get; set; }
    public required string InputHash { get; set; }
    public required string PredictionType { get; set; }
    public required string Provider { get; set; }
    public required string Model { get; set; }
    public required string PromptVersion { get; set; }
    public required string ExplanationJson { get; set; }
    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public bool FallbackUsed { get; set; }
    public string? FallbackReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

