using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using StockAnalyzer.Application.Abstractions;
using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Application.Exceptions;

namespace StockAnalyzer.Infrastructure.MlService;

public sealed class MlServiceClient(HttpClient httpClient, IConfiguration configuration) : IMlServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> SupportedModelStatuses =
        ["existing_model", "newly_trained_model", "rule_based_fallback"];
    private readonly TimeSpan predictionTimeout = TimeSpan.FromSeconds(
        Math.Max(configuration.GetValue("MlServicePredictionTimeoutSeconds", 600), 30));

    public Task<MarketOverviewDto> GetMarketOverviewAsync(
        string region,
        CancellationToken cancellationToken) =>
        GetAsync<MarketOverviewDto>(
            $"/market/overview?region={Encode(region)}",
            cancellationToken);

    public Task<StockQuotesDto> GetStockQuotesAsync(
        IReadOnlyList<string> tickers,
        CancellationToken cancellationToken) =>
        GetAsync<StockQuotesDto>(
            $"/stocks/quotes?tickers={Encode(string.Join(',', tickers))}",
            cancellationToken);

    public Task<IReadOnlyList<StockSuggestionDto>> SearchStocksAsync(
        string query,
        CancellationToken cancellationToken) =>
        GetAsync<IReadOnlyList<StockSuggestionDto>>(
            $"/stocks/search?query={Encode(query)}",
            cancellationToken);

    public Task<IReadOnlyList<HistoricalPriceDto>> GetHistoryAsync(
        string ticker,
        string period,
        CancellationToken cancellationToken) =>
        GetAsync<IReadOnlyList<HistoricalPriceDto>>(
            $"/stock/history?ticker={Encode(ticker)}&period={Encode(period)}",
            cancellationToken);

    public Task<IndicatorResponseDto> GetIndicatorsAsync(
        string ticker,
        string period,
        CancellationToken cancellationToken) =>
        GetAsync<IndicatorResponseDto>(
            $"/stock/indicators?ticker={Encode(ticker)}&period={Encode(period)}",
            cancellationToken);

    public Task<RuleBasedPredictionDto> GetRuleBasedPredictionAsync(
        string ticker,
        CancellationToken cancellationToken) =>
        GetAsync<RuleBasedPredictionDto>(
            $"/predict/rule-based?ticker={Encode(ticker)}",
            cancellationToken);

    public async Task<MlPredictionDto> GetMlPredictionAsync(
        string ticker,
        CancellationToken cancellationToken)
    {
        var result = await GetAsync<MlPredictionDto>(
            $"/predict/ml?ticker={Encode(ticker)}",
            cancellationToken,
            predictionTimeout);

        if (!SupportedModelStatuses.Contains(result.ModelStatus))
        {
            throw new ExternalServiceException(
                $"The ML service returned an unsupported model status '{result.ModelStatus}'.",
                502);
        }

        var predictionType = result.ModelStatus == "rule_based_fallback"
            ? "rule_based_fallback"
            : "ml";
        return result with
        {
            PredictionType = predictionType,
            ModelTrained = result.ModelStatus == "newly_trained_model",
            FallbackUsed = result.ModelStatus == "rule_based_fallback"
        };
    }

    public Task<ModelTrainingDto> TrainModelAsync(
        string ticker,
        string period,
        CancellationToken cancellationToken) =>
        SendAsync<ModelTrainingDto>(
            HttpMethod.Post,
            $"/models/train?ticker={Encode(ticker)}&period={Encode(period)}",
            cancellationToken,
            predictionTimeout);

    public Task<TickerModelMetricsDto> GetModelMetricsAsync(
        string ticker,
        CancellationToken cancellationToken) =>
        GetAsync<TickerModelMetricsDto>(
            $"/models/metrics?ticker={Encode(ticker)}",
            cancellationToken);

    public Task<ModelTrainingJobDto> StartTrainingJobAsync(
        string ticker,
        string period,
        CancellationToken cancellationToken) =>
        SendAsync<ModelTrainingJobDto>(
            HttpMethod.Post,
            $"/models/train/jobs?ticker={Encode(ticker)}&period={Encode(period)}",
            cancellationToken);

    public Task<ModelTrainingJobDto> GetTrainingJobAsync(
        string jobId,
        CancellationToken cancellationToken) =>
        GetAsync<ModelTrainingJobDto>(
            $"/models/train/jobs/{Encode(jobId)}",
            cancellationToken);

    public Task<ModelVersionsDto> GetModelVersionsAsync(
        string ticker,
        CancellationToken cancellationToken) =>
        GetAsync<ModelVersionsDto>(
            $"/models/versions?ticker={Encode(ticker)}",
            cancellationToken);

    private async Task<T> GetAsync<T>(
        string path,
        CancellationToken cancellationToken,
        TimeSpan? timeout = null) =>
        await SendAsync<T>(HttpMethod.Get, path, cancellationToken, timeout);

    private async Task<T> SendAsync<T>(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken,
        TimeSpan? timeout = null)
    {
        using var timeoutSource = timeout is null
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeoutSource is not null && timeout is { } requestTimeout)
        {
            timeoutSource.CancelAfter(requestTimeout);
        }
        var requestToken = timeoutSource?.Token ?? cancellationToken;

        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(method, path);
            response = await httpClient.SendAsync(request, requestToken);
        }
        catch (HttpRequestException exception)
        {
            throw new ExternalServiceException("The ML service is unavailable.", 503, exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException("The ML service request timed out.", 504, exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var detail = await ReadErrorDetailAsync(response, requestToken);
                throw new ExternalServiceException(detail, MapStatusCode(response.StatusCode));
            }

            try
            {
                return await response.Content.ReadFromJsonAsync<T>(JsonOptions, requestToken)
                    ?? throw new ExternalServiceException("The ML service returned an empty response.", 502);
            }
            catch (JsonException exception)
            {
                throw new ExternalServiceException(
                    "The ML service returned an invalid response.",
                    502,
                    exception);
            }
        }
    }

    private static async Task<string> ReadErrorDetailAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<MlError>(JsonOptions, cancellationToken);
            return error?.Detail ?? "The ML service request failed.";
        }
        catch (JsonException)
        {
            return "The ML service returned an invalid error response.";
        }
    }

    private static int MapStatusCode(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.BadRequest => 400,
        HttpStatusCode.NotFound => 404,
        HttpStatusCode.UnprocessableEntity => 422,
        _ => 502
    };

    private static string Encode(string value) => Uri.EscapeDataString(value);

    private sealed record MlError(string? Detail);
}
