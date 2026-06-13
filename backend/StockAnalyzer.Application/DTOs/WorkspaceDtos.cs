using System.Text.Json.Serialization;

namespace StockAnalyzer.Application.DTOs;

public sealed record WorkspaceWatchlistItemDto(
    string Ticker,
    [property: JsonPropertyName("addedAt")] string AddedAt,
    string Note,
    IReadOnlyList<string> Tags);

public sealed record WorkspaceAlertStateDto(
    IReadOnlyList<WorkspaceAlertRuleDto> Rules,
    IReadOnlyList<WorkspaceNotificationDto> Notifications);

public sealed record WorkspaceAlertRuleDto(
    string Id,
    string Ticker,
    string Type,
    double Threshold,
    string Frequency,
    int CooldownHours,
    string QuietStart,
    string QuietEnd,
    bool Enabled,
    string CreatedAt,
    string? LastTriggeredAt);

public sealed record WorkspaceNotificationDto(
    string Id,
    string AlertId,
    string Ticker,
    string Title,
    string Message,
    string TriggeredAt,
    string DataTimestamp,
    string EvidenceUrl,
    bool Read);

public sealed record PortfolioHoldingDto(
    string Id,
    string Ticker,
    double Quantity,
    [property: JsonPropertyName("average_cost")] double AverageCost,
    [property: JsonPropertyName("purchased_at")] string? PurchasedAt,
    string Note);
