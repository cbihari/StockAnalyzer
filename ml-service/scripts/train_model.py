#!/usr/bin/env python3
import argparse
import logging
import sys
from pathlib import Path

import pandas as pd

PROJECT_ROOT = Path(__file__).resolve().parents[1]
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

from app.dataset import FEATURE_COLUMNS, ticker_filename
from app.model_registry import ModelRegistry, normalize_ticker
from app.training import save_training_artifacts, train_random_forest

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s %(levelname)s %(name)s %(message)s",
)
logger = logging.getLogger(__name__)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Train a time-split Random Forest stock model.")
    parser.add_argument("--ticker", required=True, help="Ticker used to locate the processed CSV")
    return parser.parse_args()


def print_results(result) -> None:
    print(f"Train rows: {result.train_rows} ({result.train_start} to {result.train_end})")
    print(f"Test rows:  {result.test_rows} ({result.test_start} to {result.test_end})")
    print(f"Accuracy:  {result.accuracy:.4f}")
    print(f"Precision: {result.precision:.4f}")
    print(f"Recall:    {result.recall:.4f}")
    print("Confusion matrix [[TN, FP], [FN, TP]]:")
    print(result.confusion_matrix)
    print("Feature importance:")
    for feature, importance in result.feature_importance.items():
        print(f"  {feature:<16} {importance:.6f}")


def main() -> int:
    args = parse_args()
    normalized_ticker = normalize_ticker(args.ticker)
    ticker_key = ticker_filename(normalized_ticker)
    registry = ModelRegistry(PROJECT_ROOT)
    dataset_path = PROJECT_ROOT / "data" / "processed" / f"{ticker_key}.csv"
    model_path = registry.get_model_path(normalized_ticker)

    if not dataset_path.exists():
        logger.error(
            "Processed dataset not found at %s. Run create_dataset.py first.",
            dataset_path,
        )
        return 1

    try:
        dataset = pd.read_csv(dataset_path, parse_dates=["date"])
        model, result = train_random_forest(dataset)
        saved_model, saved_metrics = save_training_artifacts(model, result, model_path)
        saved_metadata = registry.save_model_metadata(
            ticker=normalized_ticker,
            model_name=type(model).__name__,
            training_rows=result.train_rows,
            test_rows=result.test_rows,
            accuracy=result.accuracy,
            precision=result.precision,
            recall=result.recall,
            features=FEATURE_COLUMNS,
        )
    except Exception:
        logger.exception("Model training failed ticker=%s", args.ticker)
        return 1

    print_results(result)
    print(f"Model saved:   {saved_model}")
    print(f"Metrics saved: {saved_metrics}")
    print(f"Registry saved: {registry.get_metadata_path(saved_metadata.ticker)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
