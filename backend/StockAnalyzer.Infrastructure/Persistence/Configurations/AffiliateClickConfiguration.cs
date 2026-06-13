using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockAnalyzer.Domain.Workspace;

namespace StockAnalyzer.Infrastructure.Persistence.Configurations;

public sealed class AffiliateClickConfiguration : IEntityTypeConfiguration<AffiliateClick>
{
    public void Configure(EntityTypeBuilder<AffiliateClick> builder)
    {
        builder.ToTable("AffiliateClicks");
        builder.HasKey(click => click.Id);
        builder.Property(click => click.Broker).HasMaxLength(40).IsRequired();
        builder.Property(click => click.Ticker).HasMaxLength(80);
        builder.Property(click => click.ClientId).HasMaxLength(36).IsRequired();
        builder.HasIndex(click => new { click.Broker, click.ClickedAt });
    }
}
