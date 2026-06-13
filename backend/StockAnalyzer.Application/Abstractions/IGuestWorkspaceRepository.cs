using StockAnalyzer.Domain.Workspace;

namespace StockAnalyzer.Application.Abstractions;

public interface IGuestWorkspaceRepository
{
    Task<GuestWorkspace?> GetAsync(string clientId, CancellationToken cancellationToken);
    Task SaveWatchlistAsync(string clientId, string json, CancellationToken cancellationToken);
    Task SaveAlertStateAsync(string clientId, string json, CancellationToken cancellationToken);
    Task SavePortfolioAsync(string clientId, string json, CancellationToken cancellationToken);
}
