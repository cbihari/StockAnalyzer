using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StockAnalyzer.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    public static async Task MigrateDatabaseAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StockAnalyzerDbContext>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILogger<StockAnalyzerDbContext>>();

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
                return;
            }
            catch (Exception exception) when (attempt < 10)
            {
                logger.LogWarning(exception, "Database migration attempt {Attempt} failed", attempt);
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
