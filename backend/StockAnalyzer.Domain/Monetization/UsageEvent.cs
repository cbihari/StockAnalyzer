namespace StockAnalyzer.Domain.Monetization;

public sealed class UsageEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public required string ClientId { get; set; }
    public required string FeatureKey { get; set; }
    public DateOnly UsageDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
    public int Quantity { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
