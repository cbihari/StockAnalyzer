namespace StockAnalyzer.Domain.Workspace;

public sealed class AffiliateClick
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Broker { get; set; }
    public string? Ticker { get; set; }
    public required string ClientId { get; set; }
    public DateTimeOffset ClickedAt { get; set; } = DateTimeOffset.UtcNow;
}
