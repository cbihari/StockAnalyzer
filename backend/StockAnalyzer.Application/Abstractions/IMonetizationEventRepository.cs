using StockAnalyzer.Domain.Monetization;
using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Abstractions;

public interface IMonetizationEventRepository
{
    Task AddAsync(MonetizationEvent monetizationEvent, CancellationToken cancellationToken);
    Task<MonetizationFunnelReportDto> GetFunnelAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken);
}
