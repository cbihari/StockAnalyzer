import pandas as pd
import pytest

from app.indicators import calculate_indicators, to_indicator_values


def test_indicator_calculation_preserves_history_and_produces_finite_latest_values() -> None:
    index = pd.date_range("2025-01-01", periods=80, freq="D")
    history = pd.DataFrame(
        {
            "Open": range(100, 180),
            "High": range(101, 181),
            "Low": range(99, 179),
            "Close": range(100, 180),
            "Volume": range(1000, 1080),
        },
        index=index,
    )

    result = calculate_indicators(history)
    latest = to_indicator_values(result.index[-1], result.iloc[-1])

    assert "SMA_20" not in history.columns
    assert latest.sma_20 == pytest.approx(169.5)
    assert latest.sma_50 == pytest.approx(154.5)
    assert latest.ema_20 is not None
    assert latest.ema_50 is not None
    assert latest.macd is not None
    assert latest.macd_signal is not None
    assert latest.volume_change is not None
