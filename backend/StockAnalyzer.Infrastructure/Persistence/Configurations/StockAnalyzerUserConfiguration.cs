using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockAnalyzer.Infrastructure.Identity;

namespace StockAnalyzer.Infrastructure.Persistence.Configurations;

public sealed class StockAnalyzerUserConfiguration : IEntityTypeConfiguration<StockAnalyzerUser>
{
    public void Configure(EntityTypeBuilder<StockAnalyzerUser> builder)
    {
        builder.Property(user => user.DisplayName).HasMaxLength(80).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();
    }
}
