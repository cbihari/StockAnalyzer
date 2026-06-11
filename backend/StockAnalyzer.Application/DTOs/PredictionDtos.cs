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

public sealed record PredictionEvaluationDto(
    int EvaluatedPredictions,
    int PendingPredictions);

public sealed record PredictionAccuracyDto(
    double AccuracyPercentage,
    int TotalPredictions,
    int CorrectPredictions,
    int WrongPredictions);

public sealed record PredictionExplanationDto(
    string Ticker,
    string Prediction,
    int Confidence,
    IReadOnlyList<string> TechnicalReasons,
    string RiskWarning,
    string SimpleExplanationForBeginner);
