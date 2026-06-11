using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Infrastructure.Persistence;

public sealed class StockAnalyzerDbContext(DbContextOptions<StockAnalyzerDbContext> options)
    : DbContext(options)
{
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<StockPrice> StockPrices => Set<StockPrice>();
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<ModelMetric> ModelMetrics => Set<ModelMetric>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StockAnalyzerDbContext).Assembly);
    }
}
