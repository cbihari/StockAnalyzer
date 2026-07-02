using Microsoft.Extensions.DependencyInjection;
using StockAnalyzer.Application;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.Services;

namespace StockAnalyzer.Tests.Application;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersCoreServicesAsScoped()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IStockAnalysisService)
            && descriptor.ImplementationType == typeof(StockAnalysisService)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IAiExplanationService)
            && descriptor.ImplementationType == typeof(AiExplanationService)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IMonetizationService)
            && descriptor.ImplementationType == typeof(MonetizationService)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
    }
}
