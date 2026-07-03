using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockAnalyzer.Domain.Monetization;
using StockAnalyzer.Infrastructure.Identity;

namespace StockAnalyzer.Infrastructure.Persistence.Configurations;

public sealed class MonetizationEventConfiguration : IEntityTypeConfiguration<MonetizationEvent>
{
    public void Configure(EntityTypeBuilder<MonetizationEvent> builder)
    {
        builder.ToTable("MonetizationEvents");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.ClientId).HasMaxLength(36).IsRequired();
        builder.Property(value => value.EventName).HasMaxLength(80).IsRequired();
        builder.Property(value => value.Source).HasMaxLength(80).IsRequired();
        builder.Property(value => value.FeatureKey).HasMaxLength(80);
        builder.Property(value => value.PlanKey).HasMaxLength(40);
        builder.Property(value => value.MetadataJson).HasColumnType("jsonb").IsRequired();
        builder.Property(value => value.OccurredAt).IsRequired();
        builder.HasIndex(value => new { value.EventName, value.OccurredAt });
        builder.HasIndex(value => new { value.ClientId, value.OccurredAt });
        builder.HasIndex(value => new { value.UserId, value.OccurredAt });
        builder.HasOne<StockAnalyzerUser>().WithMany().HasForeignKey(value => value.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
