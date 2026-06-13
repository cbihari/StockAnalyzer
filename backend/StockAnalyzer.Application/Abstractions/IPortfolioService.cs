using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Abstractions;

public interface IPortfolioService
{
    Task<PortfolioSummaryDto> GetSummaryAsync(Guid? userId, string clientId, CancellationToken cancellationToken);
}
