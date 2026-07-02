using StockAnalyzer.Application.DTOs;

namespace StockAnalyzer.Application.Abstractions;

public interface IPredictionEvaluationService
{
    Task<PredictionEvaluationDto> EvaluateAsync(CancellationToken cancellationToken);
    Task<PredictionAccuracyDto> GetAccuracyAsync(CancellationToken cancellationToken);
    Task<PredictionHistoryDto> GetHistoryAsync(
        Guid? userId,
        string workspaceId,
        string? ticker,
        string outcome,
        int limit,
        CancellationToken cancellationToken);

    Task<PredictionHistoryCsvExportDto> ExportHistoryCsvAsync(
        Guid? userId,
        string workspaceId,
        string? ticker,
        string outcome,
        int limit,
        CancellationToken cancellationToken);
}
