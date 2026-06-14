using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
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
        var connectionString = BuildConnectionString(
            Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Database connection string is not configured."));
        services.AddDbContext<StockAnalyzerDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IStockPriceRepository, StockPriceRepository>();
        services.AddScoped<IPredictionRepository, PredictionRepository>();
        services.AddScoped<IModelMetricRepository, ModelMetricRepository>();
        services.AddScoped<IGuestWorkspaceRepository, GuestWorkspaceRepository>();
        services.AddScoped<IAiExplanationRepository, AiExplanationRepository>();
        services.AddScoped<IAffiliateClickRepository, AffiliateClickRepository>();
        services.AddSingleton<IMarketDataProviderInfo, MarketDataProviderInfo>();
        services.AddHttpClient<IMlServiceClient, MlServiceClient>(client =>
        {
            var mlServiceUrl = Environment.GetEnvironmentVariable("ML_SERVICE_URL")
                ?? configuration["MlServiceUrl"]
                ?? "http://localhost:8000";
            client.BaseAddress = new Uri(
                mlServiceUrl.Contains("://", StringComparison.Ordinal)
                    ? mlServiceUrl
                    : $"http://{mlServiceUrl}");
            // Per-request cancellation in MlServiceClient allows first-time training more time.
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        return services;
    }

    private static string BuildConnectionString(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || (uri.Scheme != "postgres" && uri.Scheme != "postgresql"))
        {
            return value;
        }

        var credentials = uri.UserInfo.Split(':', 2);
        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty,
            SslMode = SslMode.VerifyFull,
            Pooling = true
        }.ConnectionString;
    }
}
