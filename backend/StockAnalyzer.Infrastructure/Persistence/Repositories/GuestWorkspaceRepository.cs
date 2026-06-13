using Microsoft.EntityFrameworkCore;
using System.Text.Json.Nodes;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Domain.Workspace;

namespace StockAnalyzer.Infrastructure.Persistence.Repositories;

public sealed class GuestWorkspaceRepository(StockAnalyzerDbContext dbContext) : IGuestWorkspaceRepository
{
    public Task<GuestWorkspace?> GetAsync(Guid? userId, string clientId, CancellationToken cancellationToken) =>
        dbContext.GuestWorkspaces.AsNoTracking().SingleOrDefaultAsync(
            workspace => userId.HasValue
                ? workspace.UserId == userId
                : workspace.UserId == null && workspace.ClientId == clientId,
            cancellationToken);

    public Task SaveWatchlistAsync(Guid? userId, string clientId, string json, CancellationToken cancellationToken) =>
        SaveAsync(userId, clientId, workspace => workspace.WatchlistJson = json, cancellationToken);

    public Task SaveAlertStateAsync(Guid? userId, string clientId, string json, CancellationToken cancellationToken) =>
        SaveAsync(userId, clientId, workspace => workspace.AlertStateJson = json, cancellationToken);

    public Task SavePortfolioAsync(Guid? userId, string clientId, string json, CancellationToken cancellationToken) =>
        SaveAsync(userId, clientId, workspace => workspace.PortfolioJson = json, cancellationToken);

    public async Task ClaimAsync(Guid userId, string clientId, CancellationToken cancellationToken)
    {
        var anonymous = await dbContext.GuestWorkspaces.SingleOrDefaultAsync(
            workspace => workspace.UserId == null && workspace.ClientId == clientId,
            cancellationToken);
        if (anonymous is null) return;

        var existing = await dbContext.GuestWorkspaces.SingleOrDefaultAsync(
            workspace => workspace.UserId == userId,
            cancellationToken);
        if (existing is null)
        {
            anonymous.UserId = userId;
        }
        else
        {
            existing.WatchlistJson = MergeArrayJson(anonymous.WatchlistJson, existing.WatchlistJson, "ticker");
            existing.AlertStateJson = MergeAlertStateJson(anonymous.AlertStateJson, existing.AlertStateJson);
            existing.PortfolioJson = MergeArrayJson(anonymous.PortfolioJson, existing.PortfolioJson, "id");
            existing.UpdatedAt = DateTimeOffset.UtcNow;
            dbContext.GuestWorkspaces.Remove(anonymous);
        }

        await dbContext.Predictions
            .Where(prediction => prediction.UserId == null && prediction.WorkspaceId == clientId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(prediction => prediction.UserId, userId), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string MergeArrayJson(string preferredJson, string fallbackJson, string key)
    {
        var merged = new JsonArray();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var source in new[] { preferredJson, fallbackJson })
        {
            if (JsonNode.Parse(source) is not JsonArray items) continue;
            foreach (var item in items)
            {
                var value = item?[key]?.GetValue<string>() ?? Guid.NewGuid().ToString();
                if (seen.Add(value)) merged.Add(item?.DeepClone());
            }
        }
        return merged.ToJsonString();
    }

    private static string MergeAlertStateJson(string preferredJson, string fallbackJson)
    {
        var preferred = JsonNode.Parse(preferredJson) as JsonObject ?? [];
        var fallback = JsonNode.Parse(fallbackJson) as JsonObject ?? [];
        return new JsonObject
        {
            ["rules"] = JsonNode.Parse(MergeArrayJson(
                preferred["rules"]?.ToJsonString() ?? "[]",
                fallback["rules"]?.ToJsonString() ?? "[]",
                "id")),
            ["notifications"] = JsonNode.Parse(MergeArrayJson(
                preferred["notifications"]?.ToJsonString() ?? "[]",
                fallback["notifications"]?.ToJsonString() ?? "[]",
                "id"))
        }.ToJsonString();
    }

    private async Task SaveAsync(
        Guid? userId,
        string clientId,
        Action<GuestWorkspace> update,
        CancellationToken cancellationToken)
    {
        var workspace = await dbContext.GuestWorkspaces.SingleOrDefaultAsync(
            item => userId.HasValue
                ? item.UserId == userId
                : item.UserId == null && item.ClientId == clientId,
            cancellationToken);
        if (workspace is null)
        {
            workspace = new GuestWorkspace { ClientId = clientId, UserId = userId };
            dbContext.GuestWorkspaces.Add(workspace);
        }

        update(workspace);
        workspace.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
