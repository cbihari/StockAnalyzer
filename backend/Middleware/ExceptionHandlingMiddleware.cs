using Microsoft.AspNetCore.Mvc;
using StockAnalyzer.Application.Exceptions;

namespace StockAnalyzer.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            ArgumentException => (400, "Invalid request", exception.Message),
            UnauthorizedAccessException => (403, "Forbidden", exception.Message),
            PlanLimitExceededException => (402, "Plan limit reached", exception.Message),
            ExternalServiceException external =>
                (external.StatusCode, "ML service request failed", external.Message),
            _ => (500, "Unexpected server error", "An unexpected error occurred.")
        };

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Request failed with status {StatusCode}", statusCode);
        }
        else
        {
            logger.LogWarning(exception, "Request failed with status {StatusCode}", statusCode);
        }

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions = { ["correlationId"] = context.TraceIdentifier }
        }, options: null, contentType: "application/problem+json");
    }
}
