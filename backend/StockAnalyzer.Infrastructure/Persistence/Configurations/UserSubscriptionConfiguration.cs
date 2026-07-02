using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockAnalyzer.Domain.Monetization;
using StockAnalyzer.Infrastructure.Identity;

namespace StockAnalyzer.Infrastructure.Persistence.Configurations;

public sealed class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        builder.ToTable("UserSubscriptions");
        builder.HasKey(subscription => subscription.Id);
        builder.Property(subscription => subscription.PlanKey).HasMaxLength(20).IsRequired();
        builder.Property(subscription => subscription.Status).HasMaxLength(30).IsRequired();
        builder.Property(subscription => subscription.Provider).HasMaxLength(40).IsRequired();
        builder.Property(subscription => subscription.ProviderCustomerId).HasMaxLength(120);
        builder.Property(subscription => subscription.ProviderSubscriptionId).HasMaxLength(120);
        builder.Property(subscription => subscription.ProviderCheckoutSessionId).HasMaxLength(120);
        builder.HasIndex(subscription => subscription.UserId);
        builder.HasIndex(subscription => subscription.ProviderCheckoutSessionId);
        builder.HasOne<StockAnalyzerUser>().WithMany().HasForeignKey(subscription => subscription.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
