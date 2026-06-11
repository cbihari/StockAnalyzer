import json
from dataclasses import asdict, dataclass
from pathlib import Path

import joblib
import pandas as pd
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import accuracy_score, confusion_matrix, precision_score, recall_score

from app.dataset import FEATURE_COLUMNS


@dataclass(frozen=True)
class TrainingResult:
    train_rows: int
    test_rows: int
    train_start: str
    train_end: str
    test_start: str
    test_end: str
    accuracy: float
    precision: float
    recall: float
    confusion_matrix: list[list[int]]
    feature_importance: dict[str, float]


def time_based_split(
    dataset: pd.DataFrame,
    train_fraction: float = 0.8,
) -> tuple[pd.DataFrame, pd.DataFrame]:
    if not 0 < train_fraction < 1:
        raise ValueError("train_fraction must be between 0 and 1")
    if len(dataset) < 2:
        raise ValueError("dataset must contain at least two rows")

    ordered = dataset.sort_values("date").reset_index(drop=True)
    split_index = int(len(ordered) * train_fraction)
    split_index = max(1, min(split_index, len(ordered) - 1))
    return ordered.iloc[:split_index].copy(), ordered.iloc[split_index:].copy()


def train_random_forest(dataset: pd.DataFrame) -> tuple[RandomForestClassifier, TrainingResult]:
    required_columns = {"date", "target", *FEATURE_COLUMNS}
    missing_columns = sorted(required_columns - set(dataset.columns))
    if missing_columns:
        raise ValueError(f"dataset is missing columns: {', '.join(missing_columns)}")

    train, test = time_based_split(dataset)
    model = RandomForestClassifier(
        n_estimators=300,
        max_depth=8,
        min_samples_leaf=5,
        class_weight="balanced",
        random_state=42,
        n_jobs=-1,
    )
    model.fit(train[FEATURE_COLUMNS], train["target"])
    predictions = model.predict(test[FEATURE_COLUMNS])

    importances = sorted(
        zip(FEATURE_COLUMNS, model.feature_importances_, strict=True),
        key=lambda item: item[1],
        reverse=True,
    )
    result = TrainingResult(
        train_rows=len(train),
        test_rows=len(test),
        train_start=str(train.iloc[0]["date"]),
        train_end=str(train.iloc[-1]["date"]),
        test_start=str(test.iloc[0]["date"]),
        test_end=str(test.iloc[-1]["date"]),
        accuracy=float(accuracy_score(test["target"], predictions)),
        precision=float(precision_score(test["target"], predictions, zero_division=0)),
        recall=float(recall_score(test["target"], predictions, zero_division=0)),
        confusion_matrix=confusion_matrix(test["target"], predictions, labels=[0, 1]).tolist(),
        feature_importance={name: float(value) for name, value in importances},
    )
    return model, result


def save_training_artifacts(
    model: RandomForestClassifier,
    result: TrainingResult,
    model_path: Path,
) -> tuple[Path, Path]:
    model_path.parent.mkdir(parents=True, exist_ok=True)
    joblib.dump(model, model_path)

    metrics_path = model_path.with_name(f"{model_path.stem}_metrics.json")
    metrics_path.write_text(json.dumps(asdict(result), indent=2) + "\n", encoding="utf-8")
    return model_path, metrics_path
