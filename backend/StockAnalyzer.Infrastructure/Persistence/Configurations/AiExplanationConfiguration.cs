using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Infrastructure.Persistence.Configurations;

public sealed class AiExplanationConfiguration : IEntityTypeConfiguration<AiExplanation>
{
    public void Configure(EntityTypeBuilder<AiExplanation> builder)
    {
        builder.ToTable("AiExplanations");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Ticker).HasColumnName("ticker").HasMaxLength(20).IsRequired();
        builder.Property(value => value.InputHash).HasColumnName("input_hash").HasMaxLength(64).IsRequired();
        builder.Property(value => value.PredictionType).HasColumnName("prediction_type").HasMaxLength(30).IsRequired();
        builder.Property(value => value.Provider).HasColumnName("provider").HasMaxLength(30).IsRequired();
        builder.Property(value => value.Model).HasColumnName("model").HasMaxLength(100).IsRequired();
        builder.Property(value => value.PromptVersion).HasColumnName("prompt_version").HasMaxLength(50).IsRequired();
        builder.Property(value => value.ExplanationJson).HasColumnName("explanation_json").HasColumnType("jsonb").IsRequired();
        builder.Property(value => value.InputTokens).HasColumnName("input_tokens");
        builder.Property(value => value.OutputTokens).HasColumnName("output_tokens");
        builder.Property(value => value.FallbackUsed).HasColumnName("fallback_used");
        builder.Property(value => value.FallbackReason).HasColumnName("fallback_reason").HasMaxLength(50);
        builder.Property(value => value.CreatedAt).HasColumnName("created_at");
        builder.Property(value => value.ExpiresAt).HasColumnName("expires_at");
        builder.HasIndex(value => new { value.Ticker, value.InputHash, value.Model, value.PromptVersion });
        builder.HasIndex(value => value.ExpiresAt);
    }
}

