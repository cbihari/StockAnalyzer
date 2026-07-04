using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Infrastructure.MlService;
using StockAnalyzer.Infrastructure.Payments;
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
        services.AddScoped<IUsageRepository, UsageRepository>();
        services.AddScoped<IMonetizationEventRepository, MonetizationEventRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddSingleton(_ => RazorpaySettings.FromConfiguration(configuration));
        services.AddHttpClient<RazorpayPaymentProvider>((serviceProvider, client) =>
        {
            var razorpaySettings = serviceProvider.GetRequiredService<RazorpaySettings>();
            client.BaseAddress = new Uri(razorpaySettings.BaseUrl);
        });
        services.AddHttpClient<IRazorpayCheckoutService, RazorpayCheckoutService>((serviceProvider, client) =>
        {
            var razorpaySettings = serviceProvider.GetRequiredService<RazorpaySettings>();
            client.BaseAddress = new Uri(razorpaySettings.BaseUrl);
        });
        services.AddSingleton<ManualPaymentProvider>();
        services.AddScoped<IPaymentProvider>(serviceProvider =>
        {
            var provider = (Environment.GetEnvironmentVariable("PAYMENT_PROVIDER")
                ?? configuration["Payments:Provider"]
                ?? "manual").Trim().ToLowerInvariant();
            return provider switch
            {
                "manual" => serviceProvider.GetRequiredService<ManualPaymentProvider>(),
                "razorpay" => serviceProvider.GetRequiredService<RazorpayPaymentProvider>(),
                _ => throw new InvalidOperationException("Configured payment provider is not supported.")
            };
        });
        services.AddSingleton<IMarketDataProviderInfo, MarketDataProviderInfo>();
        services.AddHttpClient<IMlServiceClient, MlServiceClient>(client =>
        {
            client.BaseAddress = BuildMlServiceUri(
                Environment.GetEnvironmentVariable("ML_SERVICE_URL") ?? configuration["MlServiceUrl"],
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
            // Per-request cancellation in MlServiceClient allows first-time training more time.
            client.Timeout = Timeout.InfiniteTimeSpan;
        });
        return services;
    }

    internal static string BuildConnectionString(string value)
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

    internal static Uri BuildMlServiceUri(string? value, string? environmentName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (string.Equals(environmentName, "Production", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("ML_SERVICE_URL is required in production.");
            }
            value = "http://localhost:8000";
        }

        return new Uri(
            value.Contains("://", StringComparison.Ordinal) ? value : $"http://{value}",
            UriKind.Absolute);
    }
}
