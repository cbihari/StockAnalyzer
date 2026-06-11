using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Infrastructure.Persistence.Configurations;

public sealed class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.ToTable("Stocks");
        builder.HasKey(stock => stock.Id);
        builder.Property(stock => stock.Ticker).HasMaxLength(20).IsRequired();
        builder.HasIndex(stock => stock.Ticker).IsUnique();
        builder.Property(stock => stock.CreatedAt).IsRequired();
    }
}
