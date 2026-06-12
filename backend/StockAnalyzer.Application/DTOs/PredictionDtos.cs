using System.Text.Json.Serialization;

namespace StockAnalyzer.Application.DTOs;

public sealed record RuleBasedPredictionDto(
    string Ticker,
    string Prediction,
    int Confidence,
    IReadOnlyList<string> Reasons);

public sealed record MlPredictionDto(
    string Ticker,
    string Prediction,
    int Confidence,
    [property: JsonPropertyName("probability_up")] double ProbabilityUp,
    [property: JsonPropertyName("probability_down")] double ProbabilityDown,
    string Model,
    [property: JsonPropertyName("latest_close")] double? LatestClose,
    IReadOnlyList<string> Reasons,
    [property: JsonPropertyName("model_status")] string ModelStatus,
    [property: JsonPropertyName("model_accuracy")] double? ModelAccuracy,
    string Warning,
    [property: JsonPropertyName("model_trained")] bool ModelTrained = false,
    [property: JsonPropertyName("fallback_used")] bool FallbackUsed = false,
    [property: JsonPropertyName("prediction_type")] string PredictionType = "ml",
    string? Reason = null,
    [property: JsonPropertyName("technical_reasons")] IReadOnlyList<string>? TechnicalReasons = null);

public sealed record ModelTrainingDto(
    string Ticker,
    string Status,
    double Accuracy,
    double Precision,
    double Recall,
    [property: JsonPropertyName("model_path")] string ModelPath,
    [property: JsonPropertyName("trained_at")] string TrainedAt);

public sealed record TickerModelMetricsDto(
    string Ticker,
    [property: JsonPropertyName("model_status")] string ModelStatus,
    [property: JsonPropertyName("model_name")] string ModelName,
    [property: JsonPropertyName("trained_at")] string TrainedAt,
    double Accuracy,
    double Precision,
    double Recall,
    [property: JsonPropertyName("confusion_matrix")] IReadOnlyList<IReadOnlyList<int>> ConfusionMatrix,
    [property: JsonPropertyName("training_rows")] int TrainingRows,
    [property: JsonPropertyName("testing_rows")] int TestingRows);

public sealed record ModelTrainingJobDto(
    [property: JsonPropertyName("job_id")] string JobId,
    string Ticker,
    string Period,
    string Status,
    [property: JsonPropertyName("submitted_at")] string SubmittedAt,
    [property: JsonPropertyName("started_at")] string? StartedAt,
    [property: JsonPropertyName("completed_at")] string? CompletedAt,
    string? Error,
    double? Accuracy,
    double? Precision,
    double? Recall,
    [property: JsonPropertyName("model_path")] string? ModelPath,
    [property: JsonPropertyName("trained_at")] string? TrainedAt);

public sealed record ModelVersionDto(
    [property: JsonPropertyName("version_id")] string VersionId,
    string Ticker,
    [property: JsonPropertyName("model_name")] string ModelName,
    [property: JsonPropertyName("model_path")] string ModelPath,
    [property: JsonPropertyName("metrics_path")] string MetricsPath,
    [property: JsonPropertyName("trained_at")] string TrainedAt,
    [property: JsonPropertyName("training_rows")] int TrainingRows,
    [property: JsonPropertyName("test_rows")] int TestRows,
    double Accuracy,
    double Precision,
    double Recall,
    IReadOnlyList<string> Features,
    [property: JsonPropertyName("confusion_matrix")] IReadOnlyList<IReadOnlyList<int>> ConfusionMatrix,
    [property: JsonPropertyName("feature_importance")] IReadOnlyDictionary<string, double> FeatureImportance,
    [property: JsonPropertyName("is_active")] bool IsActive);

public sealed record ModelVersionsDto(
    string Ticker,
    IReadOnlyList<ModelVersionDto> Versions);

public sealed record PredictionEvaluationDto(
    int EvaluatedPredictions,
    int PendingPredictions);

public sealed record PredictionAccuracyDto(
    double AccuracyPercentage,
    int TotalPredictions,
    int CorrectPredictions,
    int WrongPredictions);

public sealed record PredictionHistoryItemDto(
    Guid Id,
    string Ticker,
    string Prediction,
    int Confidence,
    [property: JsonPropertyName("probability_up")] double? ProbabilityUp,
    [property: JsonPropertyName("probability_down")] double? ProbabilityDown,
    [property: JsonPropertyName("prediction_type")] string PredictionType,
    [property: JsonPropertyName("model_status")] string? ModelStatus,
    [property: JsonPropertyName("model_accuracy")] double? ModelAccuracy,
    [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt,
    [property: JsonPropertyName("actual_result")] string? ActualResult,
    [property: JsonPropertyName("is_correct")] bool? IsCorrect);

public sealed record PredictionHistoryDto(
    IReadOnlyList<PredictionHistoryItemDto> Items,
    int Total,
    int Evaluated,
    int Pending,
    int Correct,
    int Wrong,
    [property: JsonPropertyName("accuracy_percentage")] double AccuracyPercentage);

public sealed record PredictionExplanationDto(
    string Ticker,
    string Prediction,
    int Confidence,
    IReadOnlyList<string> TechnicalReasons,
    string RiskWarning,
    string SimpleExplanationForBeginner);
