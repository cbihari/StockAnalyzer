import logging
import threading
from dataclasses import dataclass
from datetime import UTC, datetime
from pathlib import Path

from sklearn.ensemble import RandomForestClassifier

from app.dataset import FEATURE_COLUMNS, prepare_dataset, save_dataset
from app.model_registry import ModelMetadata, ModelRegistry, normalize_ticker
from app.stock_history import fetch_stock_history
from app.training import TrainingResult, save_training_artifacts, train_random_forest

logger = logging.getLogger(__name__)
PROJECT_ROOT = Path(__file__).resolve().parents[1]
MODEL_REGISTRY = ModelRegistry(PROJECT_ROOT)
MIN_HISTORICAL_ROWS = 250
REQUIRED_OHLCV_COLUMNS = {"Open", "High", "Low", "Close", "Volume"}
_training_locks: dict[str, threading.Lock] = {}
_training_locks_guard = threading.Lock()


class InsufficientTrainingDataError(ValueError):
    pass


class MissingOhlcvDataError(ValueError):
    pass


class ModelTrainingFailedError(RuntimeError):
    pass


@dataclass(frozen=True)
class ModelTrainingOutcome:
    model: RandomForestClassifier
    result: TrainingResult
    metadata: ModelMetadata


def training_lock(ticker: str) -> threading.Lock:
    normalized_ticker = normalize_ticker(ticker)
    with _training_locks_guard:
        return _training_locks.setdefault(normalized_ticker, threading.Lock())


def train_model_for_ticker(ticker: str, period: str = "5y") -> ModelTrainingOutcome:
    normalized_ticker, normalized_period, history = fetch_stock_history(ticker, period)
    missing_columns = sorted(REQUIRED_OHLCV_COLUMNS.difference(history.columns))
    if missing_columns:
        raise MissingOhlcvDataError(
            f"Historical data is missing required OHLCV columns: {', '.join(missing_columns)}."
        )
    if history[list(REQUIRED_OHLCV_COLUMNS)].isna().any().any():
        raise MissingOhlcvDataError("Historical data contains missing OHLCV values.")
    if len(history) < MIN_HISTORICAL_ROWS:
        raise InsufficientTrainingDataError(
            f"Only {len(history)} historical rows are available for {normalized_ticker} over "
            f"period '{normalized_period}'; at least {MIN_HISTORICAL_ROWS} are required."
        )

    dataset = prepare_dataset(history)
    if dataset.empty:
        raise InsufficientTrainingDataError(
            "No usable rows remain after calculating indicators and removing null values."
        )
    if dataset["target"].nunique() < 2:
        raise InsufficientTrainingDataError(
            "Historical data contains only one target class, so an ML model cannot be trained."
        )

    try:
        dataset_path = save_dataset(dataset, normalized_ticker, PROJECT_ROOT / "data" / "processed")
        model, result = train_random_forest(dataset)
        model_path, metrics_path = save_training_artifacts(
            model,
            result,
            MODEL_REGISTRY.get_model_path(normalized_ticker),
        )
        trained_at = datetime.now(UTC)
        metadata = MODEL_REGISTRY.save_model_metadata(
            ticker=normalized_ticker,
            model_name=type(model).__name__,
            training_rows=result.train_rows,
            test_rows=result.test_rows,
            accuracy=result.accuracy,
            precision=result.precision,
            recall=result.recall,
            features=FEATURE_COLUMNS,
            trained_at=trained_at,
        )
        MODEL_REGISTRY.save_model_version(
            ticker=normalized_ticker,
            model_name=type(model).__name__,
            source_model_path=model_path,
            source_metrics_path=metrics_path,
            training_rows=result.train_rows,
            test_rows=result.test_rows,
            accuracy=result.accuracy,
            precision=result.precision,
            recall=result.recall,
            features=FEATURE_COLUMNS,
            confusion_matrix=result.confusion_matrix,
            feature_importance=result.feature_importance,
            trained_at=trained_at,
        )
    except Exception as exc:
        logger.exception("Model training failed ticker=%s", normalized_ticker)
        raise ModelTrainingFailedError("The Random Forest training pipeline failed.") from exc
    logger.info(
        "Trained model ticker=%s period=%s rows=%d dataset=%s model=%s",
        normalized_ticker,
        normalized_period,
        len(dataset),
        dataset_path,
        model_path,
    )
    return ModelTrainingOutcome(model=model, result=result, metadata=metadata)
