using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StockAnalyzer.Infrastructure.Persistence;

public sealed class StockAnalyzerDbContextFactory
    : IDesignTimeDbContextFactory<StockAnalyzerDbContext>
{
    public StockAnalyzerDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres")
            ?? "Host=localhost;Port=5433;Database=stock_analyzer;Username=stock_user;Password=stock_password";
        var options = new DbContextOptionsBuilder<StockAnalyzerDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new StockAnalyzerDbContext(options);
    }
}
