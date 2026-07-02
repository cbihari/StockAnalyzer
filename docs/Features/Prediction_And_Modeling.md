# Prediction And Modeling

## Purpose

Estimate next-trading-day direction using ticker-specific Random Forest models, with deterministic rule-based fallback and visible model accountability.

## Business Flow

1. User requests analysis or prediction for a ticker.
2. ML service checks for an active model.
3. If missing, it builds a leakage-safe dataset and trains a ticker-specific model.
4. If ML cannot run, it returns a labeled rule-based fallback.
5. .NET persists prediction and metrics for accountability.

## UI Flow

- Stock detail shows prediction, confidence, model status, and fallback state.
- Model accuracy page supports metrics, retraining, job polling, and versions.
- Prediction history page shows persisted outcomes and exports CSV through the
  backend so `csv_export` plan quota can be enforced.

## Backend Flow

- .NET: `PredictionsController`, `ModelController`, `PredictionEvaluationService`, `StockAnalysisService`.
- FastAPI: `ml_prediction.py`, `rule_based_prediction.py`, `models.py`, `model_training.py`, `model_registry.py`, `training_jobs.py`.

## API Endpoints

- `GET /api/predictions/ml/{ticker}`
- `GET /api/predictions/rule-based/{ticker}`
- `GET /api/model/metrics/{ticker}`
- `POST /api/model/train/{ticker}?period=5y`
- `POST /api/model/train/{ticker}/jobs?period=5y`
- `GET /api/model/train/jobs/{jobId}`
- `GET /api/model/versions/{ticker}`

## Models

- `MlPredictionDto`, `RuleBasedPredictionDto`, `ModelTrainingDto`, `ModelTrainingJobDto`, `ModelVersionsDto`
- Domain: `Prediction`, `ModelMetric`
- ML registry metadata and metrics JSON files.

## Validation

- Ticker and period are validated.
- Training rejects insufficient or missing OHLCV data.
- Long first-time training must respect configured timeouts.

## Database Tables

- `Predictions`
- `ModelMetrics`
- `Stocks`

## Error Handling

- 422 for insufficient training data.
- 502/504 for ML service failures/timeouts.
- Fallback is valid behavior when ML cannot produce a reliable result.

## Future Improvements

- Scheduled model refresh.
- Better model drift monitoring.
- Display feature importance in beginner-friendly language.
