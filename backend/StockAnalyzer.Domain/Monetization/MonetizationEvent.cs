namespace StockAnalyzer.Domain.Monetization;

public sealed class MonetizationEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public required string ClientId { get; set; }
    public required string EventName { get; set; }
    public required string Source { get; set; }
    public string? FeatureKey { get; set; }
    public string? PlanKey { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
