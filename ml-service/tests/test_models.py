from datetime import UTC, datetime

import pytest
from fastapi import HTTPException
from sklearn.ensemble import RandomForestClassifier

from app import models
from app.model_registry import ModelMetadata, ModelRegistry
from app.model_training import (
    InsufficientTrainingDataError,
    ModelTrainingOutcome,
)
from app.training import TrainingResult


def training_outcome() -> ModelTrainingOutcome:
    result = TrainingResult(
        train_rows=1000,
        test_rows=250,
        train_start="2021-01-01",
        train_end="2025-01-01",
        test_start="2025-01-02",
        test_end="2026-01-01",
        accuracy=0.56,
        precision=0.58,
        recall=0.54,
        confusion_matrix=[[100, 25], [85, 40]],
        feature_importance={"close": 1.0},
    )
    metadata = ModelMetadata(
        ticker="TCS.NS",
        model_name="RandomForestClassifier",
        model_path="models/TCS_NS_random_forest.pkl",
        trained_at=datetime(2026, 6, 11, 10, 30, tzinfo=UTC).isoformat(),
        training_rows=1000,
        test_rows=250,
        accuracy=0.56,
        precision=0.58,
        recall=0.54,
        features=["close"],
    )
    return ModelTrainingOutcome(RandomForestClassifier(), result, metadata)


def test_train_endpoint_returns_registry_metrics(monkeypatch) -> None:
    monkeypatch.setattr(models, "train_model_for_ticker", lambda ticker, period: training_outcome())

    response = models.train_model("tcs.ns", "5y")

    assert response.model_dump() == {
        "ticker": "TCS.NS",
        "status": "trained",
        "accuracy": 0.56,
        "precision": 0.58,
        "recall": 0.54,
        "model_path": "models/TCS_NS_random_forest.pkl",
        "trained_at": "2026-06-11T10:30:00+00:00",
    }


def test_train_endpoint_returns_clear_insufficient_data_error(monkeypatch) -> None:
    def reject_training(ticker: str, period: str):
        raise InsufficientTrainingDataError("Only 20 usable rows are available; at least 100 are required.")

    monkeypatch.setattr(models, "train_model_for_ticker", reject_training)

    with pytest.raises(HTTPException) as error:
        models.train_model("NEW", "1y")

    assert error.value.status_code == 422
    assert "Only 20 usable rows" in error.value.detail


def test_model_metrics_returns_registry_and_confusion_matrix(tmp_path, monkeypatch) -> None:
    registry = ModelRegistry(tmp_path)
    monkeypatch.setattr(models, "MODEL_REGISTRY", registry)
    model_path = registry.get_model_path("AAPL")
    model_path.parent.mkdir(parents=True)
    model_path.write_bytes(b"model")
    registry.save_model_metadata(
        ticker="AAPL",
        model_name="RandomForestClassifier",
        training_rows=800,
        test_rows=200,
        accuracy=0.56,
        precision=0.58,
        recall=0.54,
        features=["close"],
    )
    model_path.with_name(f"{model_path.stem}_metrics.json").write_text(
        '{"confusion_matrix": [[67, 59], [47, 64]]}',
        encoding="utf-8",
    )

    response = models.get_model_metrics("aapl")

    assert response.ticker == "AAPL"
    assert response.model_status == "trained"
    assert response.training_rows == 800
    assert response.testing_rows == 200
    assert response.confusion_matrix == [[67, 59], [47, 64]]


def test_model_metrics_returns_requested_no_model_message(tmp_path, monkeypatch) -> None:
    monkeypatch.setattr(models, "MODEL_REGISTRY", ModelRegistry(tmp_path))

    with pytest.raises(HTTPException) as error:
        models.get_model_metrics("NVDA")

    assert error.value.status_code == 404
    assert error.value.detail == (
        "No trained model found for this ticker. Analyze or train this stock first."
    )


def test_model_versions_returns_empty_registry(tmp_path, monkeypatch) -> None:
    monkeypatch.setattr(models, "MODEL_REGISTRY", ModelRegistry(tmp_path))

    response = models.get_model_versions("msft")

    assert response.ticker == "MSFT"
    assert response.versions == []
