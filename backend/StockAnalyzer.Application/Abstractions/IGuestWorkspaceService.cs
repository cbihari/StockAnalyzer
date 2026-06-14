using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Abstractions;

public interface IGuestWorkspaceService
{
    Task<IReadOnlyList<WorkspaceWatchlistItemDto>> GetWatchlistAsync(Guid? userId, string clientId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkspaceWatchlistItemDto>> SaveWatchlistAsync(Guid? userId, string clientId, IReadOnlyList<WorkspaceWatchlistItemDto> items, CancellationToken cancellationToken);
    Task<WorkspaceAlertStateDto> GetAlertStateAsync(Guid? userId, string clientId, CancellationToken cancellationToken);
    Task<WorkspaceAlertStateDto> SaveAlertStateAsync(Guid? userId, string clientId, WorkspaceAlertStateDto state, CancellationToken cancellationToken);
    Task<IReadOnlyList<PortfolioHoldingDto>> GetPortfolioAsync(Guid? userId, string clientId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PortfolioHoldingDto>> SavePortfolioAsync(Guid? userId, string clientId, IReadOnlyList<PortfolioHoldingDto> holdings, CancellationToken cancellationToken);
    Task ClaimAsync(Guid userId, string clientId, CancellationToken cancellationToken);
}
