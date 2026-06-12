using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Application.Services;
using StockAnalyzer.Domain.Workspace;
using Xunit;

namespace StockAnalyzer.UnitTests;

public sealed class GuestWorkspaceServiceTests
{
    [Fact]
    public async Task SaveWatchlist_NormalizesAndLimitsItemDetails()
    {
        var repository = new FakeWorkspaceRepository();
        var service = new GuestWorkspaceService(repository);
        var clientId = Guid.NewGuid().ToString();
        var items = new[]
        {
            new WorkspaceWatchlistItemDto(" aapl ", "invalid", "  Research note  ", [" Growth ", "growth", "Tech"])
        };

        var saved = await service.SaveWatchlistAsync(clientId, items, default);

        Assert.Equal("AAPL", saved[0].Ticker);
        Assert.Equal("Research note", saved[0].Note);
        Assert.Equal(["growth", "tech"], saved[0].Tags);
        Assert.NotNull(repository.Workspace);
    }

    [Fact]
    public async Task WorkspaceRejectsInvalidClientId()
    {
        var service = new GuestWorkspaceService(new FakeWorkspaceRepository());
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetWatchlistAsync("not-a-uuid", default));
    }

    [Fact]
    public async Task SavePortfolio_NormalizesAndValidatesHolding()
    {
        var repository = new FakeWorkspaceRepository();
        var service = new GuestWorkspaceService(repository);

        var saved = await service.SavePortfolioAsync(Guid.NewGuid().ToString(), [
            new PortfolioHoldingDto("invalid", " aapl ", 1.23456789, 123.45678, "2026-06-01", "  Core holding  ")
        ], default);

        Assert.Equal("AAPL", saved[0].Ticker);
        Assert.Equal(1.234568, saved[0].Quantity);
        Assert.Equal(123.4568, saved[0].AverageCost);
        Assert.Equal("Core holding", saved[0].Note);
        Assert.True(Guid.TryParse(saved[0].Id, out _));
    }

    [Fact]
    public async Task SavePortfolio_RejectsNonPositiveQuantity()
    {
        var service = new GuestWorkspaceService(new FakeWorkspaceRepository());
        await Assert.ThrowsAsync<ArgumentException>(() => service.SavePortfolioAsync(Guid.NewGuid().ToString(), [
            new PortfolioHoldingDto(Guid.NewGuid().ToString(), "AAPL", 0, 100, null, "")
        ], default));
    }

    private sealed class FakeWorkspaceRepository : IGuestWorkspaceRepository
    {
        public GuestWorkspace? Workspace { get; private set; }

        public Task<GuestWorkspace?> GetAsync(string clientId, CancellationToken cancellationToken) =>
            Task.FromResult(Workspace);

        public Task SaveWatchlistAsync(string clientId, string json, CancellationToken cancellationToken)
        {
            Workspace ??= new GuestWorkspace { ClientId = clientId };
            Workspace.WatchlistJson = json;
            return Task.CompletedTask;
        }

        public Task SaveAlertStateAsync(string clientId, string json, CancellationToken cancellationToken)
        {
            Workspace ??= new GuestWorkspace { ClientId = clientId };
            Workspace.AlertStateJson = json;
            return Task.CompletedTask;
        }

        public Task SavePortfolioAsync(string clientId, string json, CancellationToken cancellationToken)
        {
            Workspace ??= new GuestWorkspace { ClientId = clientId };
            Workspace.PortfolioJson = json;
            return Task.CompletedTask;
        }
    }
}
