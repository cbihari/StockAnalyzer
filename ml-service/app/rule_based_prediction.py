import logging
from typing import Annotated, Literal

from fastapi import APIRouter, HTTPException, Query, status
from pydantic import BaseModel

from app.indicators import IndicatorValues, calculate_indicators, to_indicator_values
from app.stock_history import fetch_stock_history

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/predict", tags=["prediction"])


class RuleBasedPrediction(BaseModel):
    ticker: str
    prediction: Literal["UP", "DOWN"]
    confidence: int
    reasons: list[str]


def evaluate_rules(indicators: IndicatorValues) -> tuple[Literal["UP", "DOWN"], int, list[str]]:
    score = 0
    reasons: list[str] = []

    if indicators.rsi_14 is not None:
        if indicators.rsi_14 < 35:
            score += 1
            reasons.append(f"RSI {indicators.rsi_14:.1f} is below 35, a bullish oversold signal.")
        elif indicators.rsi_14 > 70:
            score -= 1
            reasons.append(f"RSI {indicators.rsi_14:.1f} is above 70, a bearish overbought signal.")
        else:
            reasons.append(f"RSI {indicators.rsi_14:.1f} is neutral.")

    if indicators.ema_20 is not None and indicators.ema_50 is not None:
        if indicators.ema_20 > indicators.ema_50:
            score += 1
            reasons.append("EMA 20 is above EMA 50, indicating bullish momentum.")
        else:
            score -= 1
            reasons.append("EMA 20 is at or below EMA 50, indicating bearish momentum.")

    if indicators.macd is not None and indicators.macd_signal is not None:
        if indicators.macd > indicators.macd_signal:
            score += 1
            reasons.append("MACD is above its signal line, indicating bullish momentum.")
        else:
            score -= 1
            reasons.append("MACD is at or below its signal line, indicating bearish momentum.")

    if indicators.volume_change is not None and indicators.volume_change > 0:
        if score > 0:
            score += 1
            reasons.append("Volume increased, supporting the bullish trend.")
        elif score < 0:
            score -= 1
            reasons.append("Volume increased, supporting the bearish trend.")
        else:
            reasons.append("Volume increased, but the other signals are mixed.")
    else:
        reasons.append("Volume did not increase, so it does not confirm the trend.")

    prediction: Literal["UP", "DOWN"] = "UP" if score > 0 else "DOWN"
    confidence = min(100, round(50 + (abs(score) / 4) * 50))
    return prediction, confidence, reasons


@router.get("/rule-based", response_model=RuleBasedPrediction)
def get_rule_based_prediction(
    ticker: Annotated[
        str,
        Query(
            min_length=1,
            max_length=20,
            pattern=r"^[A-Za-z0-9.^=\-]+$",
            description="Yahoo Finance ticker symbol, for example RELIANCE.NS",
        ),
    ],
) -> RuleBasedPrediction:
    normalized_ticker, _, history = fetch_stock_history(ticker, "1y")

    try:
        dataset = calculate_indicators(history)
        latest = to_indicator_values(dataset.index[-1], dataset.iloc[-1])
    except (KeyError, TypeError, ValueError, OverflowError) as exc:
        logger.exception("Rule-based indicator calculation failed ticker=%s", normalized_ticker)
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail="Unable to calculate a prediction from the returned stock history.",
        ) from exc

    required_values = (
        latest.rsi_14,
        latest.ema_20,
        latest.ema_50,
        latest.macd,
        latest.macd_signal,
        latest.volume_change,
    )
    if any(value is None for value in required_values):
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
            detail="Not enough historical data is available to calculate all prediction signals.",
        )

    prediction, confidence, reasons = evaluate_rules(latest)
    logger.info(
        "Rule-based prediction ticker=%s prediction=%s confidence=%d",
        normalized_ticker,
        prediction,
        confidence,
    )
    return RuleBasedPrediction(
        ticker=normalized_ticker,
        prediction=prediction,
        confidence=confidence,
        reasons=reasons,
    )
