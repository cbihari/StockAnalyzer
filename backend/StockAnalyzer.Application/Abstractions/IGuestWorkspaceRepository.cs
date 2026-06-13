using StockAnalyzer.Domain.Workspace;

namespace StockAnalyzer.Application.Abstractions;

public interface IGuestWorkspaceRepository
{
    Task<GuestWorkspace?> GetAsync(Guid? userId, string clientId, CancellationToken cancellationToken);
    Task SaveWatchlistAsync(Guid? userId, string clientId, string json, CancellationToken cancellationToken);
    Task SaveAlertStateAsync(Guid? userId, string clientId, string json, CancellationToken cancellationToken);
    Task SavePortfolioAsync(Guid? userId, string clientId, string json, CancellationToken cancellationToken);
    Task ClaimAsync(Guid userId, string clientId, CancellationToken cancellationToken);
}
