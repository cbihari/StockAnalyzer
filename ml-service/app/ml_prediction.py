import logging
import math
from pathlib import Path
from typing import Annotated, Literal

import joblib
import pandas as pd
from fastapi import APIRouter, HTTPException, Query, status
from pydantic import BaseModel, Field
from sklearn.ensemble import RandomForestClassifier

from app.dataset import FEATURE_COLUMNS
from app.indicators import calculate_indicators, to_indicator_values
from app.model_registry import ModelMetadata, ModelRegistry, normalize_ticker
from app.model_training import InsufficientTrainingDataError, train_model_for_ticker, training_lock
from app.model_training import MissingOhlcvDataError, ModelTrainingFailedError
from app.rule_based_prediction import evaluate_rules
from app.stock_history import fetch_stock_history

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/predict", tags=["prediction"])
PROJECT_ROOT = Path(__file__).resolve().parents[1]
MODEL_REGISTRY = ModelRegistry(PROJECT_ROOT)


class MlPredictionResponse(BaseModel):
    ticker: str
    prediction: Literal["UP", "DOWN"]
    confidence: int
    probability_up: float
    probability_down: float
    model: str
    latest_close: float | None
    reasons: list[str]
    model_status: Literal["existing_model", "newly_trained_model", "rule_based_fallback"]
    model_accuracy: float | None
    warning: str = "Educational purpose only. Not financial advice."
    model_trained: bool = False
    fallback_used: bool = False
    prediction_type: Literal["ml", "rule_based_fallback"] = "ml"
    reason: str | None = None
    technical_reasons: list[str] = Field(default_factory=list)


def latest_feature_row(history: pd.DataFrame) -> pd.DataFrame:
    dataset = calculate_indicators(history).rename(
        columns={
            "Open": "open",
            "High": "high",
            "Low": "low",
            "Close": "close",
            "Volume": "volume",
        }
    )
    latest = dataset[FEATURE_COLUMNS].tail(1).copy()

    if latest.empty:
        raise ValueError("stock history is empty")
    if latest.isna().any().any():
        raise ValueError("latest row does not contain all model features")
    if not all(math.isfinite(float(value)) for value in latest.iloc[0]):
        raise ValueError("latest row contains non-finite model features")
    return latest


def predict_with_model(
    model: RandomForestClassifier,
    features: pd.DataFrame,
) -> tuple[Literal["UP", "DOWN"], int, float, float, list[str]]:
    probabilities = model.predict_proba(features)[0]
    probabilities_by_class = {
        int(label): float(probability)
        for label, probability in zip(model.classes_, probabilities, strict=True)
    }
    probability_down = probabilities_by_class.get(0, 0.0)
    probability_up = probabilities_by_class.get(1, 0.0)
    prediction: Literal["UP", "DOWN"] = "UP" if probability_up >= probability_down else "DOWN"
    confidence = round(max(probability_up, probability_down) * 100)

    ranked_features = sorted(
        zip(FEATURE_COLUMNS, model.feature_importances_, strict=True),
        key=lambda item: item[1],
        reverse=True,
    )[:5]
    reasons = [
        f"{feature}={float(features.iloc[0][feature]):.4f} "
        f"(importance {importance:.4f})"
        for feature, importance in ranked_features
    ]
    return prediction, confidence, probability_up, probability_down, reasons


def model_path_for_ticker(ticker: str) -> Path:
    return MODEL_REGISTRY.get_model_path(ticker)


def load_or_train_model(
    ticker: str,
) -> tuple[RandomForestClassifier, Literal["existing_model", "newly_trained_model"], ModelMetadata]:
    model_path = MODEL_REGISTRY.get_model_path(ticker)
    with training_lock(ticker):
        metadata = MODEL_REGISTRY.load_model_metadata(ticker)
        if MODEL_REGISTRY.model_exists(ticker) and metadata is not None:
            try:
                model = joblib.load(model_path)
            except Exception as exc:
                logger.exception("Failed to load model ticker=%s path=%s", ticker, model_path)
                raise HTTPException(
                    status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                    detail="The trained model could not be loaded.",
                ) from exc
            if not isinstance(model, RandomForestClassifier):
                raise HTTPException(
                    status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
                    detail="The saved model is not a RandomForestClassifier.",
                )
            return model, "existing_model", metadata

        logger.info("No model found; starting on-demand training ticker=%s", ticker)
        outcome = train_model_for_ticker(ticker)
        return outcome.model, "newly_trained_model", outcome.metadata


def fallback_ticker(ticker: str) -> str:
    return ticker.strip().upper() or "UNKNOWN"


def rule_based_fallback(ticker: str, detail: str) -> MlPredictionResponse:
    normalized_ticker = fallback_ticker(ticker)
    try:
        normalized_ticker, _, history = fetch_stock_history(normalized_ticker, "1y")
        indicators = calculate_indicators(history)
        latest = to_indicator_values(indicators.index[-1], indicators.iloc[-1])
        prediction, confidence, reasons = evaluate_rules(latest)
        latest_close = float(history.iloc[-1]["Close"])
    except Exception:
        logger.warning(
            "Rule-based indicators unavailable; returning neutral fallback ticker=%s",
            normalized_ticker,
            exc_info=True,
        )
        prediction, confidence, reasons, latest_close = "DOWN", 50, [], None

    probability_up = confidence / 100 if prediction == "UP" else 1 - (confidence / 100)
    return MlPredictionResponse(
        ticker=normalized_ticker,
        prediction=prediction,
        confidence=confidence,
        probability_up=round(probability_up, 4),
        probability_down=round(1 - probability_up, 4),
        model="RuleBasedFallback",
        latest_close=latest_close,
        reasons=reasons,
        model_status="rule_based_fallback",
        model_accuracy=None,
        fallback_used=True,
        prediction_type="rule_based_fallback",
        reason=detail,
        technical_reasons=reasons,
    )


@router.get("/ml", response_model=MlPredictionResponse)
def get_ml_prediction(
    ticker: Annotated[
        str,
        Query(description="Yahoo Finance ticker symbol, for example RELIANCE.NS"),
    ],
) -> MlPredictionResponse:
    try:
        normalized_ticker = normalize_ticker(ticker)
        model, model_status, metadata = load_or_train_model(normalized_ticker)
    except (InsufficientTrainingDataError, MissingOhlcvDataError, ModelTrainingFailedError) as exc:
        reason = str(exc)
        logger.warning("Using rule-based fallback ticker=%s reason=%s", fallback_ticker(ticker), reason)
        return rule_based_fallback(ticker, reason)
    except (HTTPException, ValueError) as exc:
        reason = exc.detail if isinstance(exc, HTTPException) else str(exc)
        logger.warning("Using rule-based fallback ticker=%s reason=%s", fallback_ticker(ticker), reason)
        return rule_based_fallback(ticker, reason)
    except Exception:
        logger.exception("Unexpected model training failure ticker=%s", fallback_ticker(ticker))
        return rule_based_fallback(ticker, "ML model training failed unexpectedly.")

    try:
        _, _, history = fetch_stock_history(normalized_ticker, "1y")
        features = latest_feature_row(history)
        prediction, confidence, probability_up, probability_down, reasons = predict_with_model(
            model,
            features,
        )
    except (HTTPException, KeyError, TypeError, ValueError, OverflowError) as exc:
        logger.exception("ML prediction failed ticker=%s", normalized_ticker)
        return rule_based_fallback(
            normalized_ticker,
            "Latest stock data does not contain all features required by the ML model.",
        )

    logger.info(
        "ML prediction ticker=%s prediction=%s confidence=%d",
        normalized_ticker,
        prediction,
        confidence,
    )
    return MlPredictionResponse(
        ticker=normalized_ticker,
        prediction=prediction,
        confidence=confidence,
        probability_up=round(probability_up, 4),
        probability_down=round(probability_down, 4),
        model=type(model).__name__,
        latest_close=float(features.iloc[0]["close"]),
        reasons=reasons,
        model_status=model_status,
        model_accuracy=round(metadata.accuracy, 4),
        model_trained=model_status == "newly_trained_model",
        technical_reasons=reasons,
    )
