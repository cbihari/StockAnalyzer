using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Infrastructure.Persistence.Configurations;

public sealed class PredictionConfiguration : IEntityTypeConfiguration<Prediction>
{
    public void Configure(EntityTypeBuilder<Prediction> builder)
    {
        builder.ToTable("Predictions");
        builder.HasKey(prediction => prediction.Id);
        builder.Property(prediction => prediction.StockId).HasColumnName("stock_id");
        builder.Property(prediction => prediction.Ticker)
            .HasColumnName("ticker").HasMaxLength(20).IsRequired();
        builder.Property(prediction => prediction.PredictionType)
            .HasColumnName("prediction_type").HasMaxLength(30).IsRequired();
        builder.Property(prediction => prediction.ModelStatus)
            .HasColumnName("model_status").HasMaxLength(30);
        builder.Property(prediction => prediction.ModelAccuracy)
            .HasColumnName("model_accuracy").HasPrecision(8, 6);
        builder.Property(prediction => prediction.PredictionValue)
            .HasColumnName("prediction").HasMaxLength(10).IsRequired();
        builder.Property(prediction => prediction.Confidence)
            .HasColumnName("confidence").HasPrecision(5, 2);
        builder.Property(prediction => prediction.ProbabilityUp)
            .HasColumnName("probability_up").HasPrecision(8, 6);
        builder.Property(prediction => prediction.ProbabilityDown)
            .HasColumnName("probability_down").HasPrecision(8, 6);
        builder.Property(prediction => prediction.CreatedAt).HasColumnName("created_at");
        builder.Property(prediction => prediction.ActualResult)
            .HasColumnName("actual_result").HasMaxLength(10);
        builder.Property(prediction => prediction.IsCorrect)
            .HasColumnName("is_correct");
        builder.HasIndex(prediction => new { prediction.StockId, prediction.CreatedAt });
        builder.HasOne(prediction => prediction.Stock)
            .WithMany(stock => stock.Predictions)
            .HasForeignKey(prediction => prediction.StockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
