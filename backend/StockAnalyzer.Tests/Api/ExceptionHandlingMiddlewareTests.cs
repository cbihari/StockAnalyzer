using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using StockAnalyzer.Api.Middleware;
using StockAnalyzer.Application.Exceptions;

namespace StockAnalyzer.Tests.Api;

public sealed class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task Middleware_MapsExternalServiceExceptionToProblemDetails()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new ExternalServiceException("ML unavailable.", 503),
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/stocks/AAPL/analysis";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(503, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Contains("ML unavailable.", body);
        Assert.Contains("correlationId", body);
    }

    [Fact]
    public async Task Middleware_MapsPlanLimitExceededExceptionToPaymentRequired()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new PlanLimitExceededException("Upgrade to Pro for higher limits."),
            NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/predictions/explain-ai/AAPL";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Equal(402, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Contains("Upgrade to Pro for higher limits.", body);
        Assert.Contains("Plan limit reached", body);
    }
}
