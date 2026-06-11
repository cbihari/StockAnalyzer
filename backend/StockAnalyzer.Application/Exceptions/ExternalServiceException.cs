namespace StockAnalyzer.Application.Exceptions;

public sealed class ExternalServiceException(
    string message,
    int statusCode,
    Exception? innerException = null) : Exception(message, innerException)
{
    public int StatusCode { get; } = statusCode;
}
