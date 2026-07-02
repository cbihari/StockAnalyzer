using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockAnalyzer.Domain.Monetization;
using StockAnalyzer.Infrastructure.Identity;

namespace StockAnalyzer.Infrastructure.Persistence.Configurations;

public sealed class UsageEventConfiguration : IEntityTypeConfiguration<UsageEvent>
{
    public void Configure(EntityTypeBuilder<UsageEvent> builder)
    {
        builder.ToTable("UsageEvents");
        builder.HasKey(usage => usage.Id);
        builder.Property(usage => usage.ClientId).HasMaxLength(36).IsRequired();
        builder.Property(usage => usage.FeatureKey).HasMaxLength(80).IsRequired();
        builder.Property(usage => usage.Quantity).IsRequired();
        builder.Property(usage => usage.UsageDate).IsRequired();
        builder.Property(usage => usage.CreatedAt).IsRequired();
        builder.HasIndex(usage => new { usage.ClientId, usage.FeatureKey, usage.UsageDate });
        builder.HasIndex(usage => new { usage.UserId, usage.FeatureKey, usage.UsageDate });
        builder.HasOne<StockAnalyzerUser>().WithMany().HasForeignKey(usage => usage.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
