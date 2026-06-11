namespace StockAnalyzer.Domain.Stocks;

public sealed class Stock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Ticker { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<StockPrice> Prices { get; set; } = [];
    public ICollection<Prediction> Predictions { get; set; } = [];
    public ICollection<ModelMetric> ModelMetrics { get; set; } = [];
}
