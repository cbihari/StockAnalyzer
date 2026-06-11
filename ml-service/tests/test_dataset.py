import pandas as pd

from app.dataset import OUTPUT_COLUMNS, prepare_dataset, ticker_filename


def history_frame(rows: int = 80) -> pd.DataFrame:
    index = pd.date_range("2026-01-01", periods=rows, freq="D", tz="Asia/Kolkata")
    close = pd.Series(range(100, 100 + rows), index=index, dtype=float)
    return pd.DataFrame(
        {
            "Open": close - 1,
            "High": close + 2,
            "Low": close - 2,
            "Close": close,
            "Volume": pd.Series(range(1000, 1000 + rows), index=index, dtype=int),
        },
        index=index,
    )


def test_prepare_dataset_aligns_target_with_next_close() -> None:
    history = history_frame()
    history.iloc[60, history.columns.get_loc("Close")] = history.iloc[59]["Close"] - 5

    dataset = prepare_dataset(history)
    row = dataset.loc[dataset["date"] == history.index[59].date()].iloc[0]

    assert row["close"] == history.iloc[59]["Close"]
    assert row["target"] == 0


def test_prepare_dataset_drops_warmup_and_unknown_future_rows() -> None:
    history = history_frame()
    dataset = prepare_dataset(history)

    assert list(dataset.columns) == OUTPUT_COLUMNS
    assert not dataset.isna().any().any()
    assert dataset.iloc[0]["date"] == history.index[49].date()
    assert dataset.iloc[-1]["date"] == history.index[-2].date()
    assert set(dataset["target"].unique()) == {1}


def test_ticker_filename_is_filesystem_safe() -> None:
    assert ticker_filename("RELIANCE.NS") == "RELIANCE_NS"


def test_prepare_dataset_drops_infinite_volume_change() -> None:
    history = history_frame()
    history.iloc[60, history.columns.get_loc("Volume")] = 0

    dataset = prepare_dataset(history)

    assert history.index[61].date() not in set(dataset["date"])
