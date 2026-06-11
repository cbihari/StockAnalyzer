using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Infrastructure.Persistence.Configurations;

public sealed class StockPriceConfiguration : IEntityTypeConfiguration<StockPrice>
{
    public void Configure(EntityTypeBuilder<StockPrice> builder)
    {
        builder.ToTable("StockPrices");
        builder.HasKey(price => price.Id);
        builder.HasIndex(price => new { price.StockId, price.Date }).IsUnique();
        builder.Property(price => price.Open).HasPrecision(18, 6);
        builder.Property(price => price.High).HasPrecision(18, 6);
        builder.Property(price => price.Low).HasPrecision(18, 6);
        builder.Property(price => price.Close).HasPrecision(18, 6);
        builder.HasOne(price => price.Stock)
            .WithMany(stock => stock.Prices)
            .HasForeignKey(price => price.StockId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
