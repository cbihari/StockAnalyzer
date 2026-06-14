using System.Text.Json;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Domain.Stocks;

namespace StockAnalyzer.Application.Services;

public sealed class GuestWorkspaceService(IGuestWorkspaceRepository repository) : IGuestWorkspaceService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<WorkspaceWatchlistItemDto>> GetWatchlistAsync(
        Guid? userId,
        string clientId,
        CancellationToken cancellationToken)
    {
        var workspace = await repository.GetAsync(userId, ValidateClientId(clientId), cancellationToken);
        return workspace is null
            ? []
            : JsonSerializer.Deserialize<IReadOnlyList<WorkspaceWatchlistItemDto>>(workspace.WatchlistJson, JsonOptions) ?? [];
    }

    public async Task<IReadOnlyList<WorkspaceWatchlistItemDto>> SaveWatchlistAsync(
        Guid? userId,
        string clientId,
        IReadOnlyList<WorkspaceWatchlistItemDto> items,
        CancellationToken cancellationToken)
    {
        if (items.Count > 10) throw new ArgumentException("A guest watchlist supports up to 10 stocks.", nameof(items));
        var normalized = items.Select(item => new WorkspaceWatchlistItemDto(
            StockTicker.Create(item.Ticker).Value,
            DateTimeOffset.TryParse(item.AddedAt, out var addedAt) ? addedAt.ToUniversalTime().ToString("O") : DateTimeOffset.UtcNow.ToString("O"),
            (item.Note ?? string.Empty).Trim()[..Math.Min((item.Note ?? string.Empty).Trim().Length, 500)],
            item.Tags.Select(tag => tag.Trim().ToLowerInvariant()).Where(tag => tag.Length > 0).Distinct().Take(5).ToArray()))
            .GroupBy(item => item.Ticker, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
        await repository.SaveWatchlistAsync(userId, ValidateClientId(clientId), JsonSerializer.Serialize(normalized, JsonOptions), cancellationToken);
        return normalized;
    }

    public async Task<WorkspaceAlertStateDto> GetAlertStateAsync(Guid? userId, string clientId, CancellationToken cancellationToken)
    {
        var workspace = await repository.GetAsync(userId, ValidateClientId(clientId), cancellationToken);
        return workspace is null
            ? new WorkspaceAlertStateDto([], [])
            : JsonSerializer.Deserialize<WorkspaceAlertStateDto>(workspace.AlertStateJson, JsonOptions) ?? new WorkspaceAlertStateDto([], []);
    }

    public async Task<WorkspaceAlertStateDto> SaveAlertStateAsync(
        Guid? userId,
        string clientId,
        WorkspaceAlertStateDto state,
        CancellationToken cancellationToken)
    {
        if (state.Rules.Count > 50 || state.Notifications.Count > 100)
            throw new ArgumentException("Guest alert storage limits were exceeded.", nameof(state));
        await repository.SaveAlertStateAsync(userId, ValidateClientId(clientId), JsonSerializer.Serialize(state, JsonOptions), cancellationToken);
        return state;
    }

    public async Task<IReadOnlyList<PortfolioHoldingDto>> GetPortfolioAsync(
        Guid? userId,
        string clientId,
        CancellationToken cancellationToken)
    {
        var workspace = await repository.GetAsync(userId, ValidateClientId(clientId), cancellationToken);
        return workspace is null
            ? []
            : JsonSerializer.Deserialize<IReadOnlyList<PortfolioHoldingDto>>(workspace.PortfolioJson, JsonOptions) ?? [];
    }

    public async Task<IReadOnlyList<PortfolioHoldingDto>> SavePortfolioAsync(
        Guid? userId,
        string clientId,
        IReadOnlyList<PortfolioHoldingDto> holdings,
        CancellationToken cancellationToken)
    {
        if (holdings.Count > 50)
            throw new ArgumentException("A guest portfolio supports up to 50 holdings.", nameof(holdings));

        var normalized = holdings.Select(holding =>
        {
            if (!double.IsFinite(holding.Quantity) || holding.Quantity <= 0)
                throw new ArgumentException("Holding quantity must be greater than zero.", nameof(holdings));
            if (!double.IsFinite(holding.AverageCost) || holding.AverageCost <= 0)
                throw new ArgumentException("Average cost must be greater than zero.", nameof(holdings));
            return new PortfolioHoldingDto(
                Guid.TryParse(holding.Id, out var id) ? id.ToString() : Guid.NewGuid().ToString(),
                StockTicker.Create(holding.Ticker).Value,
                Math.Round(holding.Quantity, 6),
                Math.Round(holding.AverageCost, 4),
                DateOnly.TryParse(holding.PurchasedAt, out var purchasedAt) ? purchasedAt.ToString("O") : null,
                (holding.Note ?? string.Empty).Trim()[..Math.Min((holding.Note ?? string.Empty).Trim().Length, 300)]);
        }).ToArray();

        await repository.SavePortfolioAsync(
            userId,
            ValidateClientId(clientId),
            JsonSerializer.Serialize(normalized, JsonOptions),
            cancellationToken);
        return normalized;
    }

    public Task ClaimAsync(Guid userId, string clientId, CancellationToken cancellationToken) =>
        repository.ClaimAsync(userId, ValidateClientId(clientId), cancellationToken);

    private static string ValidateClientId(string clientId) =>
        Guid.TryParse(clientId, out var id) ? id.ToString() : throw new ArgumentException("X-Client-ID must be a valid UUID.", nameof(clientId));
}
