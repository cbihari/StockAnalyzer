using StockAnalyzer.Application.DTOs;
using StockAnalyzer.Domain.Workspace;

namespace StockAnalyzer.Application.Abstractions;

public interface IAffiliateClickRepository
{
    Task AddAsync(AffiliateClick click, CancellationToken cancellationToken);
    Task<IReadOnlyList<AffiliateClickStatDto>> GetStatsAsync(CancellationToken cancellationToken);
}
