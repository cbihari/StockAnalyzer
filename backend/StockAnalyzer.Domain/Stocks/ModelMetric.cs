namespace StockAnalyzer.Domain.Stocks;

public sealed class ModelMetric
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StockId { get; set; }
    public required Stock Stock { get; set; }
    public required string ModelName { get; set; }
    public decimal Accuracy { get; set; }
    public decimal Precision { get; set; }
    public decimal Recall { get; set; }
    public required string ConfusionMatrix { get; set; }
    public required string FeatureImportance { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
