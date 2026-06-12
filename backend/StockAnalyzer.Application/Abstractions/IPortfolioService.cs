using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Abstractions;

public interface IPortfolioService
{
    Task<PortfolioSummaryDto> GetSummaryAsync(string clientId, CancellationToken cancellationToken);
}
