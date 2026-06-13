using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Abstractions;

public interface IGuestWorkspaceService
{
    Task<IReadOnlyList<WorkspaceWatchlistItemDto>> GetWatchlistAsync(string clientId, CancellationToken cancellationToken);
    Task<IReadOnlyList<WorkspaceWatchlistItemDto>> SaveWatchlistAsync(string clientId, IReadOnlyList<WorkspaceWatchlistItemDto> items, CancellationToken cancellationToken);
    Task<WorkspaceAlertStateDto> GetAlertStateAsync(string clientId, CancellationToken cancellationToken);
    Task<WorkspaceAlertStateDto> SaveAlertStateAsync(string clientId, WorkspaceAlertStateDto state, CancellationToken cancellationToken);
    Task<IReadOnlyList<PortfolioHoldingDto>> GetPortfolioAsync(string clientId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PortfolioHoldingDto>> SavePortfolioAsync(string clientId, IReadOnlyList<PortfolioHoldingDto> holdings, CancellationToken cancellationToken);
}
