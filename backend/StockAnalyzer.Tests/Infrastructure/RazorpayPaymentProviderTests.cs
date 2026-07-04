using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using StockAnalyzer.Domain.Monetization;
using StockAnalyzer.Infrastructure.Payments;

namespace StockAnalyzer.Tests.Infrastructure;

public sealed class RazorpayPaymentProviderTests
{
    [Fact]
    public async Task CreateCheckoutSessionAsync_PostsPaymentLinkAndReturnsShortUrl()
    {
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;
        var provider = CreateProvider(new StubHandler(async request =>
        {
            capturedRequest = request;
            capturedBody = await request.Content!.ReadAsStringAsync();
            return Json("""
                {
                  "id": "plink_123",
                  "short_url": "https://rzp.io/i/test"
                }
                """);
        }));

        var result = await provider.CreateCheckoutSessionAsync(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            SubscriptionPlan.Pro,
            "https://app.stockanalyzer.test/upgrade/success",
            "https://app.stockanalyzer.test/upgrade/cancel",
            CancellationToken.None);

        Assert.Equal("razorpay", result.Provider);
        Assert.Equal("plink_123", result.ProviderSessionId);
        Assert.Equal("https://rzp.io/i/test", result.CheckoutUrl);
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Post, capturedRequest.Method);
        Assert.Equal("/v1/payment_links", capturedRequest.RequestUri!.AbsolutePath);
        Assert.Equal("Basic", capturedRequest.Headers.Authorization?.Scheme);
        Assert.Equal(
            Convert.ToBase64String(Encoding.UTF8.GetBytes("rzp_test_key:rzp_test_secret")),
            capturedRequest.Headers.Authorization?.Parameter);
        Assert.Contains("\"amount\":49900", capturedBody);
        Assert.Contains("\"currency\":\"INR\"", capturedBody);
        Assert.Contains("\"callback_url\":\"https://app.stockanalyzer.test/upgrade/success\"", capturedBody);
        Assert.Contains("\"plan_key\":\"pro\"", capturedBody);
    }

    [Fact]
    public async Task ParseWebhookAsync_ValidatesSignatureAndReturnsActiveEvent()
    {
        var provider = CreateProvider(new StubHandler(_ => Task.FromResult(Json("{}"))));
        var payload = """
            {
              "event": "payment_link.paid",
              "payload": {
                "payment_link": {
                  "entity": {
                    "id": "plink_123",
                    "status": "paid",
                    "customer_id": "cust_123",
                    "notes": {
                      "plan_key": "power"
                    }
                  }
                },
                "payment": {
                  "entity": {
                    "id": "pay_123",
                    "status": "captured"
                  }
                }
              }
            }
            """;

        var result = await provider.ParseWebhookAsync(
            payload,
            new Dictionary<string, string>
            {
                ["x-razorpay-signature"] = Signature(payload)
            },
            CancellationToken.None);

        Assert.Equal("payment_link.paid", result.EventType);
        Assert.Equal(SubscriptionStatus.Active, result.Status);
        Assert.Equal(SubscriptionPlan.Power, result.PlanKey);
        Assert.Equal("plink_123", result.ProviderCheckoutSessionId);
        Assert.Equal("pay_123", result.ProviderSubscriptionId);
        Assert.Equal("cust_123", result.ProviderCustomerId);
        Assert.NotNull(result.CurrentPeriodEnd);
    }

    [Fact]
    public async Task ParseWebhookAsync_RejectsInvalidSignature()
    {
        var provider = CreateProvider(new StubHandler(_ => Task.FromResult(Json("{}"))));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            provider.ParseWebhookAsync(
                "{\"event\":\"payment_link.paid\"}",
                new Dictionary<string, string>
                {
                    ["X-Razorpay-Signature"] = "invalid"
                },
                CancellationToken.None));
    }

    private static RazorpayPaymentProvider CreateProvider(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.razorpay.com/v1/")
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Payments:Razorpay:KeyId"] = "rzp_test_key",
                ["Payments:Razorpay:KeySecret"] = "rzp_test_secret",
                ["Payments:Razorpay:WebhookSecret"] = "webhook_secret",
                ["Payments:Razorpay:Currency"] = "INR"
            })
            .Build();
        return new RazorpayPaymentProvider(httpClient, RazorpaySettings.FromConfiguration(configuration));
    }

    private static string Signature(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("webhook_secret"));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    private static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            responseFactory(request);
    }
}
