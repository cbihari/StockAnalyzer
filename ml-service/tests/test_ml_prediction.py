import os

import numpy as np
import pandas as pd
import pytest
import joblib
from types import SimpleNamespace
from datetime import UTC, datetime
from sklearn.ensemble import RandomForestClassifier

from app.dataset import FEATURE_COLUMNS
from app import ml_prediction
from app import model_training
from app.model_registry import ModelMetadata, ModelRegistry
from app.ml_prediction import (
    InsufficientTrainingDataError,
    latest_feature_row,
    load_or_train_model,
    model_path_for_ticker,
    predict_with_model,
)


class PredictableForest(RandomForestClassifier):
    def __init__(self) -> None:
        super().__init__()
        self.classes_ = np.array([0, 1])

    @property
    def feature_importances_(self):
        return np.array(
            [0.4, 0.2, 0.1, 0.08, 0.06, 0.04, 0.03, 0.02, 0.02, 0.01, 0.01, 0.01, 0.01, 0.01]
        )

    def predict_proba(self, features):
        return np.array([[0.27, 0.73]])


def feature_frame() -> pd.DataFrame:
    return pd.DataFrame([{feature: index + 1.0 for index, feature in enumerate(FEATURE_COLUMNS)}])


def model_metadata(ticker: str) -> ModelMetadata:
    key = ticker.replace(".", "_")
    return ModelMetadata(
        ticker=ticker,
        model_name="RandomForestClassifier",
        model_path=f"models/{key}_random_forest.pkl",
        trained_at=datetime(2026, 6, 11, tzinfo=UTC).isoformat(),
        training_rows=800,
        test_rows=200,
        accuracy=0.56,
        precision=0.58,
        recall=0.54,
        features=FEATURE_COLUMNS,
    )


def test_predict_with_model_maps_probabilities_and_top_features() -> None:
    prediction, confidence, probability_up, probability_down, reasons = predict_with_model(
        PredictableForest(),
        feature_frame(),
    )

    assert prediction == "UP"
    assert confidence == 73
    assert probability_up == pytest.approx(0.73)
    assert probability_down == pytest.approx(0.27)
    assert len(reasons) == 5
    assert reasons[0].startswith("open=1.0000")


def test_latest_feature_row_rejects_warmup_nulls() -> None:
    history = pd.DataFrame(
        {
            "Open": [100.0],
            "High": [101.0],
            "Low": [99.0],
            "Close": [100.0],
            "Volume": [1000],
        },
        index=pd.DatetimeIndex(["2026-06-11"]),
    )

    with pytest.raises(ValueError, match="all model features"):
        latest_feature_row(history)


def test_model_path_uses_normalized_ticker_filename(tmp_path, monkeypatch) -> None:
    monkeypatch.setattr(ml_prediction, "MODEL_REGISTRY", ModelRegistry(tmp_path))

    assert model_path_for_ticker("TCS.NS") == tmp_path / "models" / "TCS_NS_random_forest.pkl"


def test_load_or_train_model_reuses_existing_model(tmp_path, monkeypatch) -> None:
    monkeypatch.setattr(ml_prediction, "MODEL_REGISTRY", ModelRegistry(tmp_path))
    ml_prediction._MODEL_CACHE.clear()
    model_path = model_path_for_ticker("AAPL")
    model_path.parent.mkdir(parents=True)
    joblib.dump(PredictableForest(), model_path)
    ml_prediction.MODEL_REGISTRY.save_model_metadata(
        ticker="AAPL",
        model_name="RandomForestClassifier",
        training_rows=800,
        test_rows=200,
        accuracy=0.56,
        precision=0.58,
        recall=0.54,
        features=FEATURE_COLUMNS,
    )
    monkeypatch.setattr(
        ml_prediction,
        "train_model_for_ticker",
        lambda ticker: pytest.fail(f"unexpected training for {ticker}"),
    )

    model, model_status, metadata = load_or_train_model("AAPL")

    assert isinstance(model, RandomForestClassifier)
    assert model_status == "existing_model"
    assert metadata.accuracy == pytest.approx(0.56)


def test_existing_model_is_loaded_once_until_file_changes(tmp_path, monkeypatch) -> None:
    registry = ModelRegistry(tmp_path)
    monkeypatch.setattr(ml_prediction, "MODEL_REGISTRY", registry)
    ml_prediction._MODEL_CACHE.clear()
    model_path = registry.get_model_path("AAPL")
    model_path.parent.mkdir(parents=True)
    joblib.dump(PredictableForest(), model_path)
    registry.save_model_metadata(
        ticker="AAPL",
        model_name="RandomForestClassifier",
        training_rows=800,
        test_rows=200,
        accuracy=0.56,
        precision=0.58,
        recall=0.54,
        features=FEATURE_COLUMNS,
    )
    real_load = joblib.load
    calls: list[str] = []

    def tracked_load(path):
        calls.append(str(path))
        return real_load(path)

    monkeypatch.setattr(ml_prediction.joblib, "load", tracked_load)

    first, _, _ = load_or_train_model("AAPL")
    second, _, _ = load_or_train_model("AAPL")
    current_stat = model_path.stat()
    os.utime(
        model_path,
        ns=(current_stat.st_atime_ns, current_stat.st_mtime_ns + 1_000_000),
    )
    third, _, _ = load_or_train_model("AAPL")

    assert first is second
    assert third is not second
    assert calls == [str(model_path), str(model_path)]


def test_load_or_train_model_trains_when_missing(tmp_path, monkeypatch) -> None:
    monkeypatch.setattr(ml_prediction, "MODEL_REGISTRY", ModelRegistry(tmp_path))
    calls: list[str] = []

    def fake_train(ticker: str):
        calls.append(ticker)
        return SimpleNamespace(model=PredictableForest(), metadata=model_metadata(ticker))

    monkeypatch.setattr(ml_prediction, "train_model_for_ticker", fake_train)

    model, model_status, metadata = load_or_train_model("MSFT")

    assert isinstance(model, RandomForestClassifier)
    assert model_status == "newly_trained_model"
    assert metadata.accuracy == pytest.approx(0.56)
    assert calls == ["MSFT"]


def test_train_model_rejects_fewer_than_250_historical_rows(monkeypatch) -> None:
    history = pd.DataFrame(
        {
            "Open": [100.0] * 60,
            "High": [101.0] * 60,
            "Low": [99.0] * 60,
            "Close": [100.0] * 60,
            "Volume": [1000] * 60,
        },
        index=pd.date_range("2026-01-01", periods=60),
    )
    monkeypatch.setattr(
        model_training,
        "fetch_stock_history",
        lambda ticker, period: (ticker, period, history),
    )

    with pytest.raises(InsufficientTrainingDataError, match="250"):
        model_training.train_model_for_ticker("SHORT")


def test_train_model_rejects_missing_ohlcv_columns(monkeypatch) -> None:
    history = pd.DataFrame(
        {"Open": [100.0] * 300, "High": [101.0] * 300, "Close": [100.0] * 300},
        index=pd.date_range("2025-01-01", periods=300),
    )
    monkeypatch.setattr(
        model_training,
        "fetch_stock_history",
        lambda ticker, period: (ticker, period, history),
    )

    with pytest.raises(model_training.MissingOhlcvDataError, match="Low, Volume"):
        model_training.train_model_for_ticker("BROKEN")


def test_rule_based_fallback_is_clearly_labeled(monkeypatch) -> None:
    history = pd.DataFrame(
        {"Close": [99.0, 100.0]},
        index=pd.date_range("2026-06-10", periods=2),
    )
    monkeypatch.setattr(
        ml_prediction,
        "fetch_stock_history",
        lambda ticker, period: (ticker, period, history),
    )
    monkeypatch.setattr(ml_prediction, "calculate_indicators", lambda frame: frame)
    monkeypatch.setattr(ml_prediction, "to_indicator_values", lambda index, row: object())
    monkeypatch.setattr(
        ml_prediction,
        "evaluate_rules",
        lambda indicators: ("UP", 75, ["Rule-based bullish signal."]),
    )

    response = ml_prediction.rule_based_fallback("NEW", "Only 20 usable rows are available.")

    assert response.model == "RuleBasedFallback"
    assert response.model_status == "rule_based_fallback"
    assert response.model_accuracy is None
    assert response.warning == "Educational purpose only. Not financial advice."
    assert response.fallback_used is True
    assert response.prediction_type == "rule_based_fallback"
    assert response.reason == "Only 20 usable rows are available."
    assert response.technical_reasons == ["Rule-based bullish signal."]
    assert response.probability_up == pytest.approx(0.75)


def test_ml_prediction_returns_neutral_fallback_when_training_and_rules_fail(monkeypatch) -> None:
    monkeypatch.setattr(
        ml_prediction,
        "load_or_train_model",
        lambda ticker: (_ for _ in ()).throw(RuntimeError("training crashed")),
    )
    monkeypatch.setattr(
        ml_prediction,
        "fetch_stock_history",
        lambda ticker, period: (_ for _ in ()).throw(RuntimeError("no Yahoo data")),
    )

    response = ml_prediction.get_ml_prediction("NOT-A-REAL-TICKER")

    assert response.ticker == "NOT-A-REAL-TICKER"
    assert response.prediction_type == "rule_based_fallback"
    assert response.confidence == 50
    assert response.reason == "ML model training failed unexpectedly."
    assert response.technical_reasons == []
    assert response.latest_close is None
