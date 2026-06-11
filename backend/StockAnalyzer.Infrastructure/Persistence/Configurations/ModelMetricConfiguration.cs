using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Infrastructure.Persistence.Configurations;

public sealed class ModelMetricConfiguration : IEntityTypeConfiguration<ModelMetric>
{
    public void Configure(EntityTypeBuilder<ModelMetric> builder)
    {
        builder.ToTable("ModelMetrics");
        builder.HasKey(metric => metric.Id);
        builder.Property(metric => metric.ModelName).HasMaxLength(100).IsRequired();
        builder.Property(metric => metric.Accuracy).HasPrecision(8, 6);
        builder.Property(metric => metric.Precision).HasPrecision(8, 6);
        builder.Property(metric => metric.Recall).HasPrecision(8, 6);
        builder.Property(metric => metric.ConfusionMatrix).HasColumnType("jsonb");
        builder.Property(metric => metric.FeatureImportance).HasColumnType("jsonb");
        builder.HasIndex(metric => new { metric.StockId, metric.CreatedAt });
        builder.HasOne(metric => metric.Stock)
            .WithMany(stock => stock.ModelMetrics)
            .HasForeignKey(metric => metric.StockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
