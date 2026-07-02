namespace StockAnalyzer.Application.Exceptions;

public sealed class PlanLimitExceededException(string message) : Exception(message);
