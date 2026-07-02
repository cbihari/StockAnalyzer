using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using StockAnalyzer.Application.Exceptions;
using StockAnalyzer.Infrastructure.MlService;

namespace StockAnalyzer.Tests.Infrastructure;

public sealed class MlServiceClientTests
{
    [Fact]
    public async Task SearchStocksAsync_EncodesQuery()
    {
        var handler = new StubHandler(request =>
        {
            Assert.Equal("/stocks/search?query=reliance%20%26%20sons", request.RequestUri!.PathAndQuery);
            return Json("[]");
        });
        var client = CreateClient(handler);

        var result = await client.SearchStocksAsync("reliance & sons", CancellationToken.None);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("existing_model", "ml", false, false)]
    [InlineData("newly_trained_model", "ml", true, false)]
    [InlineData("rule_based_fallback", "rule_based_fallback", false, true)]
    public async Task GetMlPredictionAsync_NormalizesModelFlags(
        string status,
        string predictionType,
        bool modelTrained,
        bool fallbackUsed)
    {
        var handler = new StubHandler(_ => Json(PredictionJson(status)));
        var client = CreateClient(handler);

        var result = await client.GetMlPredictionAsync("AAPL", CancellationToken.None);

        Assert.Equal(predictionType, result.PredictionType);
        Assert.Equal(modelTrained, result.ModelTrained);
        Assert.Equal(fallbackUsed, result.FallbackUsed);
    }

    [Fact]
    public async Task GetMlPredictionAsync_RejectsUnknownModelStatus()
    {
        var client = CreateClient(new StubHandler(_ => Json(PredictionJson("unknown"))));

        var error = await Assert.ThrowsAsync<ExternalServiceException>(() =>
            client.GetMlPredictionAsync("AAPL", CancellationToken.None));

        Assert.Equal(502, error.StatusCode);
    }

    [Fact]
    public async Task SearchStocksAsync_MapsMlErrorDetail()
    {
        var response = Json("{\"detail\":\"Ticker query is invalid.\"}");
        response.StatusCode = HttpStatusCode.BadRequest;
        var client = CreateClient(new StubHandler(_ => response));

        var error = await Assert.ThrowsAsync<ExternalServiceException>(() =>
            client.SearchStocksAsync("x", CancellationToken.None));

        Assert.Equal(400, error.StatusCode);
        Assert.Equal("Ticker query is invalid.", error.Message);
    }

    private static MlServiceClient CreateClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://ml.test") };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        return new MlServiceClient(httpClient, configuration);
    }

    private static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };

    private static string PredictionJson(string status) => $$"""
        {
          "ticker": "AAPL",
          "prediction": "UP",
          "confidence": 70,
          "probability_up": 0.7,
          "probability_down": 0.3,
          "model": "RandomForestClassifier",
          "latest_close": 200.0,
          "reasons": [],
          "model_status": "{{status}}",
          "model_accuracy": 0.6,
          "warning": "Educational purpose only. Not financial advice."
        }
        """;

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
