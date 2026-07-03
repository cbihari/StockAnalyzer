using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Application.Services;
using StockAnalyzer.Domain.Monetization;

namespace StockAnalyzer.Tests.Application;

public sealed class MonetizationServiceTests
{
    [Fact]
    public async Task CheckAsync_ReturnsUpgradePlan_WhenFreeDailyLimitIsReached()
    {
        var clientId = Guid.NewGuid().ToString();
        var repository = new FakeUsageRepository
        {
            Usage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["ai_explanation"] = 2
            }
        };
        var service = CreateService(repository);

        var result = await service.CheckAsync(
            userId: null,
            clientId,
            "ai_explanation",
            quantity: 1,
            CancellationToken.None);

        Assert.False(result.Allowed);
        Assert.Equal("free", result.Plan);
        Assert.Equal("pro", result.UpgradePlan);
        Assert.Equal(0, result.RemainingToday);
    }

    [Fact]
    public async Task RecordAsync_PersistsUsage_WhenFeatureHasRemainingQuota()
    {
        var clientId = Guid.NewGuid().ToString();
        var repository = new FakeUsageRepository();
        var service = CreateService(repository);

        var result = await service.RecordAsync(
            userId: null,
            clientId,
            "stock_analysis",
            quantity: 3,
            CancellationToken.None);

        Assert.True(result.Allowed);
        Assert.Equal(3, result.UsedToday);
        Assert.Equal(7, result.RemainingToday);
        var usage = Assert.Single(repository.Events);
        Assert.Equal("stock_analysis", usage.FeatureKey);
        Assert.Equal(3, usage.Quantity);
        Assert.Equal(clientId, usage.ClientId);
    }

    [Fact]
    public async Task RecordAsync_DoesNotPersistUsage_WhenFeatureIsOverLimit()
    {
        var clientId = Guid.NewGuid().ToString();
        var repository = new FakeUsageRepository
        {
            Usage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["csv_export"] = 0
            }
        };
        var service = CreateService(repository);

        var result = await service.RecordAsync(
            userId: null,
            clientId,
            "csv_export",
            quantity: 1,
            CancellationToken.None);

        Assert.False(result.Allowed);
        Assert.Empty(repository.Events);
        Assert.Equal("pro", result.UpgradePlan);
    }

    [Fact]
    public async Task GetStatus_UsesActiveSubscriptionPlan()
    {
        var userId = Guid.NewGuid();
        var service = CreateService(
            new FakeUsageRepository(),
            new FakeSubscriptionRepository
            {
                Current = new UserSubscription
                {
                    UserId = userId,
                    PlanKey = SubscriptionPlan.Pro,
                    Status = SubscriptionStatus.Active,
                    Provider = "manual"
                }
            });

        var result = await service.GetStatusAsync(
            userId,
            Guid.NewGuid().ToString(),
            CancellationToken.None);

        Assert.True(result.Authenticated);
        Assert.Equal("pro", result.Plan);
        Assert.Equal("active", result.Subscription?.Status);
        Assert.Contains(result.Plans, plan => plan.Key == "power");
        Assert.Contains(result.Usage, usage =>
            usage.FeatureKey == "ai_explanation" && usage.DailyLimit == 25);
    }

    [Fact]
    public async Task GetStatus_UsesFreePlan_WhenSubscriptionIsPending()
    {
        var userId = Guid.NewGuid();
        var service = CreateService(
            new FakeUsageRepository(),
            new FakeSubscriptionRepository
            {
                Current = new UserSubscription
                {
                    UserId = userId,
                    PlanKey = SubscriptionPlan.Power,
                    Status = SubscriptionStatus.Pending,
                    Provider = "manual"
                }
            });

        var result = await service.GetStatusAsync(
            userId,
            Guid.NewGuid().ToString(),
            CancellationToken.None);

        Assert.Equal("free", result.Plan);
        Assert.Equal("pending", result.Subscription?.Status);
    }

    [Fact]
    public async Task StartCheckout_SavesPendingSubscription()
    {
        var userId = Guid.NewGuid();
        var subscriptions = new FakeSubscriptionRepository();
        var service = CreateService(new FakeUsageRepository(), subscriptions);

        var result = await service.StartCheckoutAsync(
            userId,
            new CheckoutRequestDto(
                "power",
                "http://localhost:4200/upgrade/success",
                "http://localhost:4200/upgrade/cancel"),
            CancellationToken.None);

        Assert.Equal("power", result.PlanKey);
        Assert.Equal("pending", result.Status);
        Assert.Equal("manual", result.Provider);
        Assert.StartsWith("http://localhost:4200/upgrade/success?", result.CheckoutUrl);
        Assert.NotNull(subscriptions.Current);
        Assert.Equal(userId, subscriptions.Current.UserId);
        Assert.Equal("power", subscriptions.Current.PlanKey);
        Assert.Equal("pending", subscriptions.Current.Status);
        Assert.Equal(result.ProviderSessionId, subscriptions.Current.ProviderCheckoutSessionId);
    }

    [Fact]
    public async Task HandleWebhook_ActivatesPendingSubscription()
    {
        var userId = Guid.NewGuid();
        var subscription = new UserSubscription
        {
            UserId = userId,
            PlanKey = SubscriptionPlan.Pro,
            Status = SubscriptionStatus.Pending,
            Provider = "manual",
            ProviderCheckoutSessionId = "manual_session_1"
        };
        var subscriptions = new FakeSubscriptionRepository { Current = subscription };
        var service = CreateService(new FakeUsageRepository(), subscriptions);

        var result = await service.HandleWebhookAsync(
            "manual",
            """
            {
              "eventType": "subscription.active",
              "status": "active",
              "planKey": "power",
              "providerCheckoutSessionId": "manual_session_1",
              "providerSubscriptionId": "sub_123",
              "providerCustomerId": "cus_123",
              "currentPeriodEnd": "2026-08-02T00:00:00Z"
            }
            """,
            new Dictionary<string, string>(),
            CancellationToken.None);

        Assert.Equal("manual", result.Provider);
        Assert.Equal("active", result.Status);
        Assert.Equal("power", result.PlanKey);
        Assert.Equal("active", subscriptions.Current?.Status);
        Assert.Equal("power", subscriptions.Current?.PlanKey);
        Assert.Equal("sub_123", subscriptions.Current?.ProviderSubscriptionId);
        Assert.Equal("cus_123", subscriptions.Current?.ProviderCustomerId);
        Assert.Equal(DateTimeOffset.Parse("2026-08-02T00:00:00Z"), subscriptions.Current?.CurrentPeriodEnd);
    }

    [Fact]
    public async Task HandleWebhook_RejectsUnknownProvider()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.HandleWebhookAsync(
                "stripe",
                "{}",
                new Dictionary<string, string>(),
                CancellationToken.None));
    }

    [Fact]
    public async Task CheckAsync_RejectsInvalidClientId()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CheckAsync(null, "not-a-guid", "stock_analysis", 1, CancellationToken.None));
    }

    [Fact]
    public async Task CheckStoredLimitAsync_ReturnsUpgradePlan_WhenFreeWatchlistLimitIsExceeded()
    {
        var service = CreateService();

        var result = await service.CheckStoredLimitAsync(
            null,
            Guid.NewGuid().ToString(),
            "watchlist_item",
            11,
            CancellationToken.None);

        Assert.False(result.Allowed);
        Assert.Equal("free", result.Plan);
        Assert.Equal(10, result.StoredLimit);
        Assert.Equal("pro", result.UpgradePlan);
    }

    [Fact]
    public async Task CheckStoredLimitAsync_AllowsStoredTotalWithinPlan()
    {
        var userId = Guid.NewGuid();
        var service = CreateService(
            subscriptionRepository: new FakeSubscriptionRepository
            {
                Current = new UserSubscription
                {
                    UserId = userId,
                    PlanKey = SubscriptionPlan.Pro,
                    Status = SubscriptionStatus.Active,
                    Provider = "manual"
                }
            });

        var result = await service.CheckStoredLimitAsync(
            userId,
            Guid.NewGuid().ToString(),
            "alert_rule",
            50,
            CancellationToken.None);

        Assert.True(result.Allowed);
        Assert.Equal("pro", result.Plan);
        Assert.Equal(50, result.StoredLimit);
        Assert.Null(result.UpgradePlan);
    }

    [Fact]
    public async Task RecordEventAsync_PersistsBoundedMonetizationEvent()
    {
        var clientId = Guid.NewGuid().ToString();
        var events = new FakeMonetizationEventRepository();
        var service = CreateService(eventRepository: events);

        var result = await service.RecordEventAsync(
            null,
            clientId,
            new MonetizationEventRequestDto(
                "quota_callout_click",
                "stock_detail",
                "ai_explanation",
                "pro",
                new Dictionary<string, string>
                {
                    ["reason"] = "limit_reached",
                    ["long_value"] = new string('a', 200)
                }),
            CancellationToken.None);

        Assert.Equal("quota_callout_click", result.EventName);
        var recorded = Assert.Single(events.Events);
        Assert.Equal(clientId, recorded.ClientId);
        Assert.Equal("stock_detail", recorded.Source);
        Assert.Equal("ai_explanation", recorded.FeatureKey);
        Assert.Equal("pro", recorded.PlanKey);
        Assert.Contains("\"reason\":\"limit_reached\"", recorded.MetadataJson);
        Assert.DoesNotContain(new string('a', 121), recorded.MetadataJson);
    }

    [Fact]
    public async Task RecordEventAsync_RejectsUnsupportedEventName()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.RecordEventAsync(
            null,
            Guid.NewGuid().ToString(),
            new MonetizationEventRequestDto("ticker_search", "search"),
            CancellationToken.None));
    }

    private static MonetizationService CreateService(
        FakeUsageRepository? usageRepository = null,
        FakeSubscriptionRepository? subscriptionRepository = null,
        FakeMonetizationEventRepository? eventRepository = null,
        FakePaymentProvider? paymentProvider = null) =>
        new(
            usageRepository ?? new FakeUsageRepository(),
            subscriptionRepository ?? new FakeSubscriptionRepository(),
            eventRepository ?? new FakeMonetizationEventRepository(),
            paymentProvider ?? new FakePaymentProvider());

    private sealed class FakeUsageRepository : IUsageRepository
    {
        public IReadOnlyDictionary<string, int> Usage { get; init; } =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public List<UsageEvent> Events { get; } = [];

        public Task<IReadOnlyDictionary<string, int>> GetDailyUsageAsync(
            Guid? userId,
            string clientId,
            DateOnly usageDate,
            CancellationToken cancellationToken) =>
            Task.FromResult(Usage);

        public Task AddAsync(UsageEvent usageEvent, CancellationToken cancellationToken)
        {
            Events.Add(usageEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSubscriptionRepository : ISubscriptionRepository
    {
        public UserSubscription? Current { get; set; }

        public Task<UserSubscription?> GetCurrentForUserAsync(
            Guid userId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Current?.UserId == userId ? Current : null);

        public Task<UserSubscription?> FindByProviderReferenceAsync(
            string provider,
            string? providerCheckoutSessionId,
            string? providerSubscriptionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Current is not null &&
                Current.Provider == provider &&
                ((!string.IsNullOrWhiteSpace(providerCheckoutSessionId) &&
                    Current.ProviderCheckoutSessionId == providerCheckoutSessionId) ||
                 (!string.IsNullOrWhiteSpace(providerSubscriptionId) &&
                    Current.ProviderSubscriptionId == providerSubscriptionId))
                    ? Current
                    : null);

        public Task SaveAsync(UserSubscription subscription, CancellationToken cancellationToken)
        {
            Current = subscription;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMonetizationEventRepository : IMonetizationEventRepository
    {
        public List<MonetizationEvent> Events { get; } = [];

        public Task AddAsync(MonetizationEvent monetizationEvent, CancellationToken cancellationToken)
        {
            Events.Add(monetizationEvent);
            return Task.CompletedTask;
        }

        public Task<MonetizationFunnelReportDto> GetFunnelAsync(
            DateOnly from,
            DateOnly to,
            CancellationToken cancellationToken) =>
            Task.FromResult(new MonetizationFunnelReportDto(from, to, Events.Count, [], []));
    }

    private sealed class FakePaymentProvider : IPaymentProvider
    {
        public string Name => "manual";

        public Task<PaymentCheckoutSessionDto> CreateCheckoutSessionAsync(
            Guid userId,
            string planKey,
            string successUrl,
            string cancelUrl,
            CancellationToken cancellationToken)
        {
            var sessionId = $"manual_{Guid.NewGuid():N}";
            return Task.FromResult(new PaymentCheckoutSessionDto(
                Name,
                sessionId,
                $"{successUrl}?checkoutSession={sessionId}&plan={planKey}&provider=manual"));
        }

        public Task<PaymentWebhookEventDto> ParseWebhookAsync(
            string payload,
            IReadOnlyDictionary<string, string> headers,
            CancellationToken cancellationToken)
        {
            using var document = System.Text.Json.JsonDocument.Parse(payload);
            var root = document.RootElement;
            string? GetString(string name) => root.TryGetProperty(name, out var value) ? value.GetString() : null;
            DateTimeOffset? GetDate(string name) =>
                root.TryGetProperty(name, out var value) && value.TryGetDateTimeOffset(out var parsed)
                    ? parsed
                    : null;
            return Task.FromResult(new PaymentWebhookEventDto(
                GetString("eventId") ?? "evt_test",
                GetString("eventType") ?? "subscription.active",
                GetString("status") ?? "active",
                GetString("planKey"),
                GetString("providerCheckoutSessionId"),
                GetString("providerSubscriptionId"),
                GetString("providerCustomerId"),
                GetDate("currentPeriodStart"),
                GetDate("currentPeriodEnd"),
                false));
        }
    }
}
