using Microsoft.Extensions.DependencyInjection;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.Services;

namespace StockAnalyzer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IStockAnalysisService, StockAnalysisService>();
        services.AddScoped<IPredictionEvaluationService, PredictionEvaluationService>();
        services.AddScoped<IPredictionExplanationService, PredictionExplanationService>();
        services.AddScoped<IGuestWorkspaceService, GuestWorkspaceService>();
        services.AddScoped<IPortfolioService, PortfolioService>();
        services.AddScoped<IAiExplanationService, AiExplanationService>();
        services.AddScoped<IAiResearchService, AiResearchService>();
        return services;
    }
}
