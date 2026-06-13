namespace StockAnalyzer.Domain.Workspace;

public sealed class GuestWorkspace
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string ClientId { get; set; }
    public Guid? UserId { get; set; }
    public string WatchlistJson { get; set; } = "[]";
    public string AlertStateJson { get; set; } = "{\"rules\":[],\"notifications\":[]}";
    public string PortfolioJson { get; set; } = "[]";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
