using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Domain.Workspace;

namespace StockAnalyzer.Infrastructure.Persistence.Repositories;

public sealed class GuestWorkspaceRepository(StockAnalyzerDbContext dbContext) : IGuestWorkspaceRepository
{
    public Task<GuestWorkspace?> GetAsync(string clientId, CancellationToken cancellationToken) =>
        dbContext.GuestWorkspaces.AsNoTracking().SingleOrDefaultAsync(
            workspace => workspace.ClientId == clientId,
            cancellationToken);

    public Task SaveWatchlistAsync(string clientId, string json, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "GuestWorkspaces"
                ("Id", "ClientId", "WatchlistJson", "AlertStateJson", "PortfolioJson", "CreatedAt", "UpdatedAt")
            VALUES
                ({id}, {clientId}, CAST({json} AS jsonb), CAST({DefaultAlertStateJson} AS jsonb), CAST({DefaultPortfolioJson} AS jsonb), {timestamp}, {timestamp})
            ON CONFLICT ("ClientId") DO UPDATE
            SET "WatchlistJson" = EXCLUDED."WatchlistJson",
                "UpdatedAt" = EXCLUDED."UpdatedAt";
            """, cancellationToken);
    }

    public Task SaveAlertStateAsync(string clientId, string json, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "GuestWorkspaces"
                ("Id", "ClientId", "WatchlistJson", "AlertStateJson", "PortfolioJson", "CreatedAt", "UpdatedAt")
            VALUES
                ({id}, {clientId}, CAST({DefaultWatchlistJson} AS jsonb), CAST({json} AS jsonb), CAST({DefaultPortfolioJson} AS jsonb), {timestamp}, {timestamp})
            ON CONFLICT ("ClientId") DO UPDATE
            SET "AlertStateJson" = EXCLUDED."AlertStateJson",
                "UpdatedAt" = EXCLUDED."UpdatedAt";
            """, cancellationToken);
    }

    public Task SavePortfolioAsync(string clientId, string json, CancellationToken cancellationToken)
    {
        var id = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        return dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "GuestWorkspaces"
                ("Id", "ClientId", "WatchlistJson", "AlertStateJson", "PortfolioJson", "CreatedAt", "UpdatedAt")
            VALUES
                ({id}, {clientId}, CAST({DefaultWatchlistJson} AS jsonb), CAST({DefaultAlertStateJson} AS jsonb), CAST({json} AS jsonb), {timestamp}, {timestamp})
            ON CONFLICT ("ClientId") DO UPDATE
            SET "PortfolioJson" = EXCLUDED."PortfolioJson",
                "UpdatedAt" = EXCLUDED."UpdatedAt";
            """, cancellationToken);
    }

    private const string DefaultWatchlistJson = "[]";
    private const string DefaultAlertStateJson = "{\"rules\":[],\"notifications\":[]}";
    private const string DefaultPortfolioJson = "[]";
}
