using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Infrastructure.MlService;
using StockAnalyzer.Infrastructure.Persistence;
using StockAnalyzer.Infrastructure.Persistence.Repositories;

namespace StockAnalyzer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<StockAnalyzerDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres")));
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IStockPriceRepository, StockPriceRepository>();
        services.AddScoped<IPredictionRepository, PredictionRepository>();
        services.AddScoped<IModelMetricRepository, ModelMetricRepository>();
        services.AddSingleton<IMarketDataProviderInfo, MarketDataProviderInfo>();
        services.AddHttpClient<IMlServiceClient, MlServiceClient>(client =>
        {
            client.BaseAddress = new Uri(
                configuration["MlServiceUrl"] ?? "http://localhost:8000");
            // Per-request cancellation in MlServiceClient allows first-time training more time.
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        return services;
    }
}
