from pathlib import Path

import numpy as np
import pandas as pd

from app.indicators import calculate_indicators
from app.model_registry import ticker_key

FEATURE_COLUMNS = [
    "open",
    "high",
    "low",
    "close",
    "volume",
    "daily_return",
    "SMA_20",
    "SMA_50",
    "EMA_20",
    "EMA_50",
    "RSI_14",
    "MACD",
    "MACD_signal",
    "volume_change",
]
OUTPUT_COLUMNS = ["date", *FEATURE_COLUMNS, "target"]


def prepare_dataset(history: pd.DataFrame) -> pd.DataFrame:
    enriched = calculate_indicators(history).rename(
        columns={
            "Open": "open",
            "High": "high",
            "Low": "low",
            "Close": "close",
            "Volume": "volume",
        }
    )

    # Features at time t contain only information available through time t.
    # The label uses the next close and is assigned only after features exist.
    next_close = enriched["close"].shift(-1)
    enriched["target"] = (next_close > enriched["close"]).astype("Int64")
    enriched.loc[next_close.isna(), "target"] = pd.NA

    dataset = enriched.reset_index()
    date_column = dataset.columns[0]
    dataset = dataset.rename(columns={date_column: "date"})
    dataset["date"] = pd.to_datetime(dataset["date"]).dt.date

    dataset = dataset[OUTPUT_COLUMNS].replace([np.inf, -np.inf], pd.NA).dropna().copy()
    dataset["volume"] = dataset["volume"].astype("int64")
    dataset["target"] = dataset["target"].astype("int8")
    return dataset.reset_index(drop=True)


def ticker_filename(ticker: str) -> str:
    return ticker_key(ticker)


def save_dataset(dataset: pd.DataFrame, ticker: str, output_dir: Path) -> Path:
    output_dir.mkdir(parents=True, exist_ok=True)
    output_path = output_dir / f"{ticker_filename(ticker)}.csv"
    dataset.to_csv(output_path, index=False)
    return output_path
