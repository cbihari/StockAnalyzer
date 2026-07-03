using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Domain.Monetization;
using System.Text.Json;

namespace StockAnalyzer.Application.Services;

public sealed class MonetizationService(
    IUsageRepository usageRepository,
    ISubscriptionRepository subscriptionRepository,
    IMonetizationEventRepository eventRepository,
    IPaymentProvider paymentProvider) : IMonetizationService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> AllowedEvents = new(StringComparer.Ordinal)
    {
        "quota_callout_view",
        "quota_callout_click",
        "checkout_start",
        "checkout_created",
        "checkout_failed",
        "paid_feature_attempt"
    };

    private static readonly IReadOnlyList<PlanDefinition> Plans =
    [
        new(
            SubscriptionPlan.Free,
            "Free",
            "Core educational research with starter limits.",
            "INR 0",
            [
                new("stock_analysis", "Stock analyses", 10, null, "analysis"),
                new("ai_explanation", "AI explanations", 2, null, "explanation"),
                new("watchlist_item", "Watchlist stocks", null, 10, "stock"),
                new("alert_rule", "Alert rules", null, 5, "alert"),
                new("portfolio_holding", "Portfolio holdings", null, 10, "holding"),
                new("csv_export", "CSV exports", 0, null, "export")
            ]),
        new(
            SubscriptionPlan.Pro,
            "Pro",
            "Higher limits for active research workflows.",
            "INR 299-499/month",
            [
                new("stock_analysis", "Stock analyses", 100, null, "analysis"),
                new("ai_explanation", "AI explanations", 25, null, "explanation"),
                new("watchlist_item", "Watchlist stocks", null, 50, "stock"),
                new("alert_rule", "Alert rules", null, 50, "alert"),
                new("portfolio_holding", "Portfolio holdings", null, 50, "holding"),
                new("csv_export", "CSV exports", 10, null, "export")
            ]),
        new(
            SubscriptionPlan.Power,
            "Power",
            "Advanced limits for power users and creator workflows.",
            "INR 999/month",
            [
                new("stock_analysis", "Stock analyses", 300, null, "analysis"),
                new("ai_explanation", "AI explanations", 100, null, "explanation"),
                new("watchlist_item", "Watchlist stocks", null, 200, "stock"),
                new("alert_rule", "Alert rules", null, 200, "alert"),
                new("portfolio_holding", "Portfolio holdings", null, 200, "holding"),
                new("csv_export", "CSV exports", 50, null, "export")
            ])
    ];

    private static readonly IReadOnlyDictionary<string, PlanDefinition> PlansByKey =
        Plans.ToDictionary(plan => plan.Key, StringComparer.Ordinal);

    public async Task<MonetizationStatusDto> GetStatusAsync(
        Guid? userId,
        string clientId,
        CancellationToken cancellationToken)
    {
        var normalizedClientId = ValidateClientId(clientId);
        var subscription = userId.HasValue
            ? await subscriptionRepository.GetCurrentForUserAsync(userId.Value, cancellationToken)
            : null;
        var plan = ResolvePlan(subscription);
        var usage = await usageRepository.GetDailyUsageAsync(
            userId,
            normalizedClientId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken);

        return new MonetizationStatusDto(
            plan.Key,
            userId.HasValue,
            subscription is null ? null : ToSubscriptionDto(subscription),
            Plans.Select(ToDto).ToArray(),
            plan.Limits.Select(limit => ToUsageDto(limit, usage.GetValueOrDefault(limit.FeatureKey))).ToArray());
    }

    public async Task<UsageCheckDto> CheckAsync(
        Guid? userId,
        string clientId,
        string featureKey,
        int quantity,
        CancellationToken cancellationToken)
    {
        var normalizedFeature = NormalizeFeature(featureKey);
        var normalizedQuantity = ValidateQuantity(quantity);
        var normalizedClientId = ValidateClientId(clientId);
        var subscription = userId.HasValue
            ? await subscriptionRepository.GetCurrentForUserAsync(userId.Value, cancellationToken)
            : null;
        var plan = ResolvePlan(subscription);
        var limit = FindLimit(plan, normalizedFeature);
        var usage = await usageRepository.GetDailyUsageAsync(
            userId,
            normalizedClientId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken);
        return BuildCheck(plan, limit, normalizedQuantity, usage.GetValueOrDefault(normalizedFeature));
    }

    public async Task<UsageCheckDto> RecordAsync(
        Guid? userId,
        string clientId,
        string featureKey,
        int quantity,
        CancellationToken cancellationToken)
    {
        var normalizedFeature = NormalizeFeature(featureKey);
        var normalizedQuantity = ValidateQuantity(quantity);
        var normalizedClientId = ValidateClientId(clientId);
        var check = await CheckAsync(
            userId,
            normalizedClientId,
            normalizedFeature,
            normalizedQuantity,
            cancellationToken);
        if (!check.Allowed)
        {
            return check;
        }

        await usageRepository.AddAsync(new UsageEvent
        {
            UserId = userId,
            ClientId = normalizedClientId,
            FeatureKey = normalizedFeature,
            Quantity = normalizedQuantity
        }, cancellationToken);

        return check with
        {
            UsedToday = check.UsedToday + normalizedQuantity,
            RemainingToday = check.DailyLimit is null
                ? null
                : Math.Max(0, check.DailyLimit.Value - check.UsedToday - normalizedQuantity),
            Message = "Usage recorded."
        };
    }

    public async Task<MonetizationEventResponseDto> RecordEventAsync(
        Guid? userId,
        string clientId,
        MonetizationEventRequestDto request,
        CancellationToken cancellationToken)
    {
        var normalizedClientId = ValidateClientId(clientId);
        var eventName = NormalizeEventName(request.EventName);
        var source = NormalizeSource(request.Source);
        var featureKey = string.IsNullOrWhiteSpace(request.FeatureKey)
            ? null
            : NormalizeFeature(request.FeatureKey);
        var planKey = string.IsNullOrWhiteSpace(request.PlanKey)
            ? null
            : SubscriptionPlan.Normalize(request.PlanKey);
        var metadata = NormalizeMetadata(request.Metadata);

        await eventRepository.AddAsync(new MonetizationEvent
        {
            UserId = userId,
            ClientId = normalizedClientId,
            EventName = eventName,
            Source = source,
            FeatureKey = featureKey,
            PlanKey = planKey,
            MetadataJson = JsonSerializer.Serialize(metadata, JsonOptions)
        }, cancellationToken);

        return new MonetizationEventResponseDto(eventName, "Event recorded.");
    }

    public async Task<StoredLimitCheckDto> CheckStoredLimitAsync(
        Guid? userId,
        string clientId,
        string featureKey,
        int requestedTotal,
        CancellationToken cancellationToken)
    {
        var normalizedFeature = NormalizeFeature(featureKey);
        var normalizedTotal = ValidateStoredTotal(requestedTotal);
        ValidateClientId(clientId);
        var subscription = userId.HasValue
            ? await subscriptionRepository.GetCurrentForUserAsync(userId.Value, cancellationToken)
            : null;
        var plan = ResolvePlan(subscription);
        var limit = FindLimit(plan, normalizedFeature);
        return BuildStoredCheck(plan, limit, normalizedTotal);
    }

    public async Task<CheckoutResponseDto> StartCheckoutAsync(
        Guid userId,
        CheckoutRequestDto request,
        CancellationToken cancellationToken)
    {
        var planKey = SubscriptionPlan.Normalize(request.PlanKey);
        if (planKey == SubscriptionPlan.Free)
        {
            throw new ArgumentException("Checkout is available only for paid plans.", nameof(request));
        }

        ValidateCheckoutUrl(request.SuccessUrl, nameof(request.SuccessUrl));
        ValidateCheckoutUrl(request.CancelUrl, nameof(request.CancelUrl));

        var session = await paymentProvider.CreateCheckoutSessionAsync(
            userId,
            planKey,
            request.SuccessUrl,
            request.CancelUrl,
            cancellationToken);
        var subscription = await subscriptionRepository.GetCurrentForUserAsync(userId, cancellationToken)
            ?? new UserSubscription
            {
                UserId = userId,
                PlanKey = planKey,
                Status = SubscriptionStatus.Pending,
                Provider = session.Provider
            };

        subscription.PlanKey = planKey;
        subscription.Status = SubscriptionStatus.Pending;
        subscription.Provider = session.Provider;
        subscription.ProviderCheckoutSessionId = session.ProviderSessionId;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        await subscriptionRepository.SaveAsync(subscription, cancellationToken);

        return new CheckoutResponseDto(
            planKey,
            subscription.Status,
            session.Provider,
            session.CheckoutUrl,
            session.ProviderSessionId,
            "Checkout session created. Plan access begins after the payment provider confirms the subscription.");
    }

    public async Task<PaymentWebhookResultDto> HandleWebhookAsync(
        string provider,
        string payload,
        IReadOnlyDictionary<string, string> headers,
        CancellationToken cancellationToken)
    {
        var normalizedProvider = NormalizeProvider(provider);
        if (!string.Equals(normalizedProvider, paymentProvider.Name, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Payment provider is not configured for this environment.", nameof(provider));
        }

        var webhook = await paymentProvider.ParseWebhookAsync(payload, headers, cancellationToken);
        var subscription = await subscriptionRepository.FindByProviderReferenceAsync(
            normalizedProvider,
            webhook.ProviderCheckoutSessionId,
            webhook.ProviderSubscriptionId,
            cancellationToken);
        if (subscription is null)
        {
            throw new ArgumentException("Subscription was not found for this payment event.", nameof(payload));
        }

        var status = SubscriptionStatus.Normalize(webhook.Status);
        var planKey = string.IsNullOrWhiteSpace(webhook.PlanKey)
            ? subscription.PlanKey
            : SubscriptionPlan.Normalize(webhook.PlanKey);
        if (planKey == SubscriptionPlan.Free && status == SubscriptionStatus.Active)
        {
            throw new ArgumentException("Paid subscription webhook cannot activate the free plan.", nameof(payload));
        }

        subscription.PlanKey = planKey;
        subscription.Status = status;
        subscription.Provider = normalizedProvider;
        subscription.ProviderCustomerId = webhook.ProviderCustomerId ?? subscription.ProviderCustomerId;
        subscription.ProviderSubscriptionId = webhook.ProviderSubscriptionId ?? subscription.ProviderSubscriptionId;
        subscription.ProviderCheckoutSessionId = webhook.ProviderCheckoutSessionId ?? subscription.ProviderCheckoutSessionId;
        subscription.CurrentPeriodStart = webhook.CurrentPeriodStart ?? subscription.CurrentPeriodStart;
        subscription.CurrentPeriodEnd = webhook.CurrentPeriodEnd ?? subscription.CurrentPeriodEnd;
        subscription.CancelAtPeriodEnd = webhook.CancelAtPeriodEnd;
        subscription.UpdatedAt = DateTimeOffset.UtcNow;
        await subscriptionRepository.SaveAsync(subscription, cancellationToken);

        return new PaymentWebhookResultDto(
            normalizedProvider,
            webhook.EventType,
            subscription.PlanKey,
            subscription.Status,
            subscription.ProviderCheckoutSessionId ?? subscription.ProviderSubscriptionId ?? string.Empty,
            "Subscription state updated from payment webhook.");
    }

    private static SubscriptionPlanDto ToDto(PlanDefinition plan) => new(
        plan.Key,
        plan.Name,
        plan.Description,
        plan.PriceLabel,
        plan.Limits.Select(limit => new PlanLimitDto(
            limit.FeatureKey,
            limit.Label,
            limit.DailyLimit,
            limit.StoredLimit,
            limit.Unit)).ToArray());

    private static UsageFeatureDto ToUsageDto(PlanLimit limit, int usedToday)
    {
        int? remaining = limit.DailyLimit is null
            ? null
            : Math.Max(0, limit.DailyLimit.Value - usedToday);
        return new UsageFeatureDto(
            limit.FeatureKey,
            limit.Label,
            usedToday,
            limit.DailyLimit,
            remaining,
            limit.StoredLimit,
            limit.DailyLimit is null || remaining.GetValueOrDefault() > 0,
            limit.Unit);
    }

    private static SubscriptionStateDto ToSubscriptionDto(UserSubscription subscription) => new(
        subscription.PlanKey,
        subscription.Status,
        subscription.Provider,
        subscription.CurrentPeriodEnd,
        subscription.CancelAtPeriodEnd);

    private static UsageCheckDto BuildCheck(
        PlanDefinition plan,
        PlanLimit limit,
        int quantity,
        int usedToday)
    {
        int? remaining = limit.DailyLimit is null
            ? null
            : Math.Max(0, limit.DailyLimit.Value - usedToday);
        var allowed = limit.DailyLimit is null || usedToday + quantity <= limit.DailyLimit.Value;
        var upgradePlan = allowed
            ? null
            : NextPlan(
                plan.Key,
                limit.FeatureKey,
                candidate => candidate.DailyLimit is null || candidate.DailyLimit > 0);
        var message = allowed
            ? "Feature is available for the current plan."
            : upgradePlan is null
                ? "This plan limit has been reached."
                : $"This plan limit has been reached. Upgrade to {PlansByKey[upgradePlan].Name} for higher limits.";
        return new UsageCheckDto(
            limit.FeatureKey,
            limit.Label,
            plan.Key,
            quantity,
            usedToday,
            limit.DailyLimit,
            remaining,
            allowed,
            upgradePlan,
            message);
    }

    private static StoredLimitCheckDto BuildStoredCheck(
        PlanDefinition plan,
        PlanLimit limit,
        int requestedTotal)
    {
        var allowed = limit.StoredLimit is null || requestedTotal <= limit.StoredLimit.Value;
        var upgradePlan = allowed
            ? null
            : NextPlan(
                plan.Key,
                limit.FeatureKey,
                candidate => candidate.StoredLimit is null || requestedTotal <= candidate.StoredLimit.Value);
        var message = allowed
            ? "Stored feature limit is available for the current plan."
            : upgradePlan is null
                ? "This plan limit has been reached."
                : $"This plan limit has been reached. Upgrade to {PlansByKey[upgradePlan].Name} for higher limits.";
        return new StoredLimitCheckDto(
            limit.FeatureKey,
            limit.Label,
            plan.Key,
            requestedTotal,
            limit.StoredLimit,
            allowed,
            upgradePlan,
            message);
    }

    private static PlanDefinition ResolvePlan(UserSubscription? subscription)
    {
        var configured = Environment.GetEnvironmentVariable("STOCKANALYZER_PLAN_OVERRIDE");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return PlansByKey[SubscriptionPlan.Normalize(configured)];
        }

        if (subscription?.Status == SubscriptionStatus.Active)
        {
            return PlansByKey[SubscriptionPlan.Normalize(subscription.PlanKey)];
        }

        return PlansByKey[SubscriptionPlan.Free];
    }

    private static PlanLimit FindLimit(PlanDefinition plan, string featureKey) =>
        plan.Limits.SingleOrDefault(limit => limit.FeatureKey == featureKey)
        ?? throw new ArgumentException("Unknown monetized feature.", nameof(featureKey));

    private static string? NextPlan(string currentPlan, string featureKey, Func<PlanLimit, bool> accepts)
    {
        var currentIndex = Plans.ToList().FindIndex(plan => plan.Key == currentPlan);
        foreach (var plan in Plans.Skip(currentIndex + 1))
        {
            var limit = plan.Limits.Single(item => item.FeatureKey == featureKey);
            if (accepts(limit))
            {
                return plan.Key;
            }
        }

        return null;
    }

    private static string NormalizeFeature(string featureKey)
    {
        var normalized = (featureKey ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length is < 3 or > 80 || normalized.Any(ch => !char.IsLetterOrDigit(ch) && ch != '_' && ch != '-'))
        {
            throw new ArgumentException("Feature key is invalid.", nameof(featureKey));
        }

        return normalized;
    }

    private static string NormalizeEventName(string eventName)
    {
        var normalized = NormalizeToken(eventName, "Event name", 3, 80);
        if (!AllowedEvents.Contains(normalized))
        {
            throw new ArgumentException("Analytics event is not supported.", nameof(eventName));
        }

        return normalized;
    }

    private static string NormalizeSource(string source) =>
        NormalizeToken(source, "Event source", 3, 80);

    private static string NormalizeToken(string value, string label, int minLength, int maxLength)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length < minLength ||
            normalized.Length > maxLength ||
            normalized.Any(ch => !char.IsLetterOrDigit(ch) && ch != '_' && ch != '-'))
        {
            throw new ArgumentException($"{label} is invalid.", nameof(value));
        }

        return normalized;
    }

    private static IReadOnlyDictionary<string, string> NormalizeMetadata(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        return metadata
            .Take(10)
            .Select(pair => new
            {
                Key = NormalizeToken(pair.Key, "Metadata key", 1, 40),
                Value = (pair.Value ?? string.Empty).Trim()[..Math.Min((pair.Value ?? string.Empty).Trim().Length, 120)]
            })
            .Where(pair => pair.Value.Length > 0)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private static int ValidateQuantity(int quantity) =>
        quantity is >= 1 and <= 100 ? quantity : throw new ArgumentException("Quantity must be between 1 and 100.", nameof(quantity));

    private static int ValidateStoredTotal(int requestedTotal) =>
        requestedTotal is >= 0 and <= 1_000
            ? requestedTotal
            : throw new ArgumentException("Stored total must be between 0 and 1000.", nameof(requestedTotal));

    private static string ValidateClientId(string clientId) =>
        Guid.TryParse(clientId, out var id) ? id.ToString() : throw new ArgumentException("X-Client-ID must be a valid UUID.", nameof(clientId));

    private static string NormalizeProvider(string provider)
    {
        var normalized = (provider ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized.Length is < 3 or > 40 || normalized.Any(ch => !char.IsLetterOrDigit(ch) && ch != '_' && ch != '-'))
        {
            throw new ArgumentException("Payment provider is invalid.", nameof(provider));
        }

        return normalized;
    }

    private static void ValidateCheckoutUrl(string value, string parameterName)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException("Checkout redirect URL must be an absolute HTTP or HTTPS URL.", parameterName);
        }
    }

    private sealed record PlanDefinition(
        string Key,
        string Name,
        string Description,
        string PriceLabel,
        IReadOnlyList<PlanLimit> Limits);

    private sealed record PlanLimit(
        string FeatureKey,
        string Label,
        int? DailyLimit,
        int? StoredLimit,
        string Unit);
}
