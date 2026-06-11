namespace StockAnalyzer.Domain.Stocks;

public sealed class StockPrice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StockId { get; set; }
    public required Stock Stock { get; set; }
    public DateOnly Date { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}
