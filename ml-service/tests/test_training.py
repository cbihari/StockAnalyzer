import joblib
import pandas as pd
import pytest

from app.dataset import FEATURE_COLUMNS
from app.training import save_training_artifacts, time_based_split, train_random_forest


def training_dataset(rows: int = 100) -> pd.DataFrame:
    data = {
        "date": pd.date_range("2025-01-01", periods=rows, freq="D"),
        "target": [index % 2 for index in range(rows)],
    }
    for position, feature in enumerate(FEATURE_COLUMNS, start=1):
        data[feature] = [index * position for index in range(rows)]
    return pd.DataFrame(data)


def test_time_based_split_preserves_chronology() -> None:
    dataset = training_dataset().sample(frac=1, random_state=7)
    train, test = time_based_split(dataset)

    assert len(train) == 80
    assert len(test) == 20
    assert train["date"].max() < test["date"].min()


def test_training_returns_metrics_and_ranked_importances() -> None:
    model, result = train_random_forest(training_dataset())

    assert list(model.feature_names_in_) == FEATURE_COLUMNS
    assert 0 <= result.accuracy <= 1
    assert 0 <= result.precision <= 1
    assert 0 <= result.recall <= 1
    assert len(result.confusion_matrix) == 2
    importances = list(result.feature_importance.values())
    assert importances == sorted(importances, reverse=True)
    assert sum(importances) == pytest.approx(1.0)


def test_training_artifacts_can_be_loaded(tmp_path) -> None:
    model, result = train_random_forest(training_dataset())
    model_path, metrics_path = save_training_artifacts(model, result, tmp_path / "model.pkl")

    loaded_model = joblib.load(model_path)
    assert list(loaded_model.feature_names_in_) == FEATURE_COLUMNS
    assert metrics_path.exists()
    assert '"accuracy"' in metrics_path.read_text(encoding="utf-8")
