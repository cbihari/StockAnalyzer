using System.Net;
using System.Security.Cryptography;
using System.Text;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Domain.Monetization;
using StockAnalyzer.Infrastructure.Payments;

namespace StockAnalyzer.Tests.Infrastructure;

public sealed class RazorpayCheckoutServiceTests
{
    [Fact]
    public async Task CreateOrderAsync_PostsOrderAndReturnsOnlyCheckoutSafeKey()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var repository = new FakeSubscriptionRepository();
        var service = CreateService(repository, async request =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return Json("""
                {
                  "id": "order_123",
                  "amount": 49900,
                  "currency": "INR"
                }
                """);
        });

        var result = await service.CreateOrderAsync(
            UserId,
            "Shiv",
            "shiv@example.test",
            new RazorpayOrderRequestDto(SubscriptionPlan.Pro),
            CancellationToken.None);

        Assert.Equal("rzp_test_key", result.KeyId);
        Assert.DoesNotContain("secret", result.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal("order_123", result.OrderId);
        Assert.Equal(RazorpaySettings.TestMode, result.CheckoutMode);
        Assert.Equal(49900, result.Amount);
        Assert.Equal(SubscriptionStatus.Pending, repository.Current?.Status);
        Assert.Equal("order_123", repository.Current?.ProviderCheckoutSessionId);
        Assert.Equal(HttpMethod.Post, capturedRequest?.Method);
        Assert.Equal("/v1/orders", capturedRequest?.RequestUri?.AbsolutePath);
        Assert.Equal(
            Convert.ToBase64String(Encoding.UTF8.GetBytes("rzp_test_key:rzp_test_secret")),
            capturedRequest?.Headers.Authorization?.Parameter);
        Assert.Contains("\"amount\":49900", capturedBody);
        Assert.Contains("\"plan_key\":\"pro\"", capturedBody);
    }

    [Fact]
    public async Task VerifyPaymentAsync_UsesStoredOrderSignatureAndActivatesSubscription()
    {
        var repository = new FakeSubscriptionRepository
        {
            Current = PendingSubscription("order_123")
        };
        var service = CreateService(repository, request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/v1/payments/pay_123", request.RequestUri!.AbsolutePath);
            return Task.FromResult(Json("""
                {
                  "id": "pay_123",
                  "order_id": "order_123",
                  "amount": 49900,
                  "currency": "INR",
                  "status": "captured"
                }
                """));
        });

        var result = await service.VerifyPaymentAsync(
            UserId,
            new RazorpayPaymentVerificationRequestDto(
                "pay_123",
                "order_123",
                null,
                Signature("order_123|pay_123")),
            CancellationToken.None);

        Assert.Equal(SubscriptionStatus.Active, result.Status);
        Assert.Equal(SubscriptionStatus.Active, repository.Current?.Status);
        Assert.Equal("pay_123", repository.Current?.ProviderSubscriptionId);
        Assert.Equal("order_123", repository.Current?.ProviderCheckoutSessionId);
        Assert.NotNull(repository.Current?.CurrentPeriodEnd);
    }

    [Fact]
    public async Task VerifyPaymentAsync_RejectsTamperedOrderSignature()
    {
        var repository = new FakeSubscriptionRepository
        {
            Current = PendingSubscription("order_123")
        };
        var service = CreateService(repository, _ => Task.FromResult(Json("{}")));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.VerifyPaymentAsync(
                UserId,
                new RazorpayPaymentVerificationRequestDto(
                    "pay_123",
                    "order_123",
                    null,
                    Signature("order_tampered|pay_123")),
                CancellationToken.None));
    }

    [Fact]
    public async Task VerifyPaymentAsync_RejectsUncapturedPayment()
    {
        var repository = new FakeSubscriptionRepository
        {
            Current = PendingSubscription("order_123")
        };
        var service = CreateService(repository, _ => Task.FromResult(Json("""
            {
              "id": "pay_123",
              "order_id": "order_123",
              "amount": 49900,
              "currency": "INR",
              "status": "authorized"
            }
            """)));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.VerifyPaymentAsync(
                UserId,
                new RazorpayPaymentVerificationRequestDto(
                    "pay_123",
                    "order_123",
                    null,
                    Signature("order_123|pay_123")),
                CancellationToken.None));
    }

    [Fact]
    public void RazorpaySettings_RejectsLiveKeyWhenModeIsTest()
    {
        var settings = new RazorpaySettings
        {
            KeyId = "rzp_live_key",
            KeySecret = "secret",
            Mode = RazorpaySettings.TestMode
        };

        var exception = Assert.Throws<InvalidOperationException>(settings.EnsureApiCredentials);
        Assert.Contains("RAZORPAY_MODE=live", exception.Message);
    }

    [Fact]
    public void RazorpaySettings_AcceptsLiveKeyWhenModeIsLive()
    {
        var settings = new RazorpaySettings
        {
            KeyId = "rzp_live_key",
            KeySecret = "secret",
            Mode = RazorpaySettings.LiveMode
        };

        settings.EnsureApiCredentials();
    }

    [Fact]
    public void RazorpaySettings_RejectsTestKeyWhenModeIsLive()
    {
        var settings = new RazorpaySettings
        {
            KeyId = "rzp_test_key",
            KeySecret = "secret",
            Mode = RazorpaySettings.LiveMode
        };

        var exception = Assert.Throws<InvalidOperationException>(settings.EnsureApiCredentials);
        Assert.Contains("rzp_live_", exception.Message);
    }

    private static RazorpayCheckoutService CreateService(
        FakeSubscriptionRepository repository,
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
    {
        var httpClient = new HttpClient(new StubHandler(responseFactory))
        {
            BaseAddress = new Uri("https://api.razorpay.com/v1/")
        };
        return new RazorpayCheckoutService(
            httpClient,
            new RazorpaySettings
            {
                KeyId = "rzp_test_key",
                KeySecret = "rzp_test_secret",
                Currency = "INR",
                ProAmountPaise = 49900,
                PowerAmountPaise = 99900
            },
            repository);
    }

    private static UserSubscription PendingSubscription(string orderId) => new()
    {
        UserId = UserId,
        PlanKey = SubscriptionPlan.Pro,
        Status = SubscriptionStatus.Pending,
        Provider = "razorpay",
        ProviderCheckoutSessionId = orderId
    };

    private static string Signature(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("rzp_test_secret"));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };

    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

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
            Task.FromResult(
                Current is not null &&
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

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            responseFactory(request);
    }
}
