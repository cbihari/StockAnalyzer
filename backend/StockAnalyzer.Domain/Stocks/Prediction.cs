namespace StockAnalyzer.Domain.Stocks;

public sealed class Prediction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? UserId { get; set; }
    public string? WorkspaceId { get; set; }
    public Guid StockId { get; set; }
    public required Stock Stock { get; set; }
    public required string Ticker { get; set; }
    public required string PredictionType { get; set; }
    public string? ModelStatus { get; set; }
    public decimal? ModelAccuracy { get; set; }
    public required string PredictionValue { get; set; }
    public decimal Confidence { get; set; }
    public decimal? ProbabilityUp { get; set; }
    public decimal? ProbabilityDown { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? ActualResult { get; set; }
    public bool? IsCorrect { get; set; }
}
