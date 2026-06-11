from datetime import date

import pandas as pd
from fastapi.testclient import TestClient

from app.indicators import IndicatorValues
from app.main import app
from app.rule_based_prediction import evaluate_rules


def indicators(
    *,
    rsi: float = 50,
    ema_20: float = 110,
    ema_50: float = 100,
    macd: float = 2,
    macd_signal: float = 1,
    volume_change: float = 0.1,
) -> IndicatorValues:
    return IndicatorValues(
        date=date(2026, 6, 11),
        daily_return=0.01,
        sma_20=105,
        sma_50=100,
        ema_20=ema_20,
        ema_50=ema_50,
        rsi_14=rsi,
        macd=macd,
        macd_signal=macd_signal,
        bollinger_upper=120,
        bollinger_lower=90,
        volume_change=volume_change,
    )


def test_bullish_rules_return_up_with_full_confidence() -> None:
    prediction, confidence, reasons = evaluate_rules(indicators(rsi=30))

    assert prediction == "UP"
    assert confidence == 100
    assert len(reasons) == 4
    assert any("bullish oversold" in reason for reason in reasons)


def test_bearish_rules_return_down_with_full_confidence() -> None:
    prediction, confidence, reasons = evaluate_rules(
        indicators(rsi=75, ema_20=90, ema_50=100, macd=-2, macd_signal=-1)
    )

    assert prediction == "DOWN"
    assert confidence == 100
    assert any("bearish overbought" in reason for reason in reasons)


def test_mixed_rules_return_down_at_low_confidence() -> None:
    prediction, confidence, reasons = evaluate_rules(
        indicators(ema_20=90, ema_50=100, volume_change=-0.1)
    )

    assert prediction == "DOWN"
    assert confidence == 50
    assert any("does not confirm" in reason for reason in reasons)


def test_endpoint_returns_rule_based_prediction(monkeypatch) -> None:
    index = pd.DatetimeIndex(["2026-06-11"])
    history = pd.DataFrame(
        {"Open": [100], "High": [110], "Low": [95], "Close": [108], "Volume": [1000]},
        index=index,
    )
    calculated = history.assign(
        daily_return=0.01,
        SMA_20=105.0,
        SMA_50=100.0,
        EMA_20=110.0,
        EMA_50=100.0,
        RSI_14=30.0,
        MACD=2.0,
        MACD_signal=1.0,
        bollinger_upper=120.0,
        bollinger_lower=90.0,
        volume_change=0.2,
    )

    monkeypatch.setattr(
        "app.rule_based_prediction.fetch_stock_history",
        lambda ticker, period: (ticker.upper(), period, history),
    )
    monkeypatch.setattr("app.rule_based_prediction.calculate_indicators", lambda _: calculated)

    response = TestClient(app).get("/predict/rule-based?ticker=reliance.ns")

    assert response.status_code == 200
    assert response.json() == {
        "ticker": "RELIANCE.NS",
        "prediction": "UP",
        "confidence": 100,
        "reasons": [
            "RSI 30.0 is below 35, a bullish oversold signal.",
            "EMA 20 is above EMA 50, indicating bullish momentum.",
            "MACD is above its signal line, indicating bullish momentum.",
            "Volume increased, supporting the bullish trend.",
        ],
    }
