namespace StockAnalyzer.Application.DTOs;

public sealed record PlanLimitDto(
    string FeatureKey,
    string Label,
    int? DailyLimit,
    int? StoredLimit,
    string Unit);

public sealed record SubscriptionPlanDto(
    string Key,
    string Name,
    string Description,
    string PriceLabel,
    IReadOnlyList<PlanLimitDto> Limits);

public sealed record UsageFeatureDto(
    string FeatureKey,
    string Label,
    int UsedToday,
    int? DailyLimit,
    int? RemainingToday,
    int? StoredLimit,
    bool Allowed,
    string Unit);

public sealed record MonetizationStatusDto(
    string Plan,
    bool Authenticated,
    SubscriptionStateDto? Subscription,
    IReadOnlyList<SubscriptionPlanDto> Plans,
    IReadOnlyList<UsageFeatureDto> Usage);

public sealed record UsageCheckDto(
    string FeatureKey,
    string Label,
    string Plan,
    int RequestedQuantity,
    int UsedToday,
    int? DailyLimit,
    int? RemainingToday,
    bool Allowed,
    string? UpgradePlan,
    string Message);

public sealed record RecordUsageRequestDto(string FeatureKey, int Quantity = 1);

public sealed record SubscriptionStateDto(
    string Plan,
    string Status,
    string Provider,
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd);

public sealed record CheckoutRequestDto(
    string PlanKey,
    string SuccessUrl,
    string CancelUrl);

public sealed record CheckoutResponseDto(
    string PlanKey,
    string Status,
    string Provider,
    string CheckoutUrl,
    string ProviderSessionId,
    string Message);

public sealed record PaymentCheckoutSessionDto(
    string Provider,
    string ProviderSessionId,
    string CheckoutUrl);

public sealed record PaymentWebhookEventDto(
    string EventId,
    string EventType,
    string Status,
    string? PlanKey,
    string? ProviderCheckoutSessionId,
    string? ProviderSubscriptionId,
    string? ProviderCustomerId,
    DateTimeOffset? CurrentPeriodStart,
    DateTimeOffset? CurrentPeriodEnd,
    bool CancelAtPeriodEnd);

public sealed record PaymentWebhookResultDto(
    string Provider,
    string EventType,
    string PlanKey,
    string Status,
    string ProviderSessionId,
    string Message);
