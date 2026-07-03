using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Domain.Stocks;
using StockAnalyzer.Domain.Monetization;
using StockAnalyzer.Domain.Workspace;
using StockAnalyzer.Infrastructure.Identity;

namespace StockAnalyzer.Infrastructure.Persistence;

public sealed class StockAnalyzerDbContext(DbContextOptions<StockAnalyzerDbContext> options)
    : IdentityDbContext<StockAnalyzerUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<StockPrice> StockPrices => Set<StockPrice>();
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<ModelMetric> ModelMetrics => Set<ModelMetric>();
    public DbSet<GuestWorkspace> GuestWorkspaces => Set<GuestWorkspace>();
    public DbSet<AiExplanation> AiExplanations => Set<AiExplanation>();
    public DbSet<AffiliateClick> AffiliateClicks => Set<AffiliateClick>();
    public DbSet<UsageEvent> UsageEvents => Set<UsageEvent>();
    public DbSet<MonetizationEvent> MonetizationEvents => Set<MonetizationEvent>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StockAnalyzerDbContext).Assembly);
    }
}
