import logging
import math
from datetime import date
from typing import Annotated

import pandas as pd
from fastapi import APIRouter, HTTPException, Query, status
from pydantic import BaseModel, ConfigDict, Field

from app.stock_history import fetch_stock_history, number

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/stock", tags=["stock"])


class IndicatorValues(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    date: date
    daily_return: float | None
    sma_20: float | None = Field(serialization_alias="SMA_20")
    sma_50: float | None = Field(serialization_alias="SMA_50")
    ema_20: float | None = Field(serialization_alias="EMA_20")
    ema_50: float | None = Field(serialization_alias="EMA_50")
    rsi_14: float | None = Field(serialization_alias="RSI_14")
    macd: float | None = Field(serialization_alias="MACD")
    macd_signal: float | None = Field(serialization_alias="MACD_signal")
    bollinger_upper: float | None
    bollinger_lower: float | None
    volume_change: float | None


class IndicatorRow(IndicatorValues):
    open: float
    high: float
    low: float
    close: float
    volume: int


class IndicatorResponse(BaseModel):
    ticker: str
    period: str
    latest: IndicatorValues
    data: list[IndicatorRow]


def calculate_indicators(history: pd.DataFrame) -> pd.DataFrame:
    result = history.copy()
    close = result["Close"].astype(float)
    volume = result["Volume"].astype(float)

    result["daily_return"] = close.pct_change()
    result["SMA_20"] = close.rolling(window=20, min_periods=20).mean()
    result["SMA_50"] = close.rolling(window=50, min_periods=50).mean()
    result["EMA_20"] = close.ewm(span=20, adjust=False, min_periods=20).mean()
    result["EMA_50"] = close.ewm(span=50, adjust=False, min_periods=50).mean()

    delta = close.diff()
    average_gain = delta.clip(lower=0).ewm(alpha=1 / 14, adjust=False, min_periods=14).mean()
    average_loss = (-delta.clip(upper=0)).ewm(alpha=1 / 14, adjust=False, min_periods=14).mean()
    relative_strength = average_gain / average_loss
    result["RSI_14"] = 100 - (100 / (1 + relative_strength))

    ema_12 = close.ewm(span=12, adjust=False, min_periods=12).mean()
    ema_26 = close.ewm(span=26, adjust=False, min_periods=26).mean()
    result["MACD"] = ema_12 - ema_26
    result["MACD_signal"] = result["MACD"].ewm(span=9, adjust=False, min_periods=9).mean()

    standard_deviation = close.rolling(window=20, min_periods=20).std()
    result["bollinger_upper"] = result["SMA_20"] + (2 * standard_deviation)
    result["bollinger_lower"] = result["SMA_20"] - (2 * standard_deviation)
    result["volume_change"] = volume.pct_change()

    return result


def optional_number(value: object) -> float | None:
    result = float(value)
    return result if math.isfinite(result) else None


def to_indicator_values(timestamp: object, row: pd.Series) -> IndicatorValues:
    return IndicatorValues(
        date=pd.Timestamp(timestamp).date(),
        daily_return=optional_number(row["daily_return"]),
        sma_20=optional_number(row["SMA_20"]),
        sma_50=optional_number(row["SMA_50"]),
        ema_20=optional_number(row["EMA_20"]),
        ema_50=optional_number(row["EMA_50"]),
        rsi_14=optional_number(row["RSI_14"]),
        macd=optional_number(row["MACD"]),
        macd_signal=optional_number(row["MACD_signal"]),
        bollinger_upper=optional_number(row["bollinger_upper"]),
        bollinger_lower=optional_number(row["bollinger_lower"]),
        volume_change=optional_number(row["volume_change"]),
    )


@router.get("/indicators", response_model=IndicatorResponse)
def get_stock_indicators(
    ticker: Annotated[
        str,
        Query(
            min_length=1,
            max_length=20,
            pattern=r"^[A-Za-z0-9.^=\-]+$",
            description="Yahoo Finance ticker symbol, for example RELIANCE.NS",
        ),
    ],
    period: Annotated[str, Query(description="Historical lookback period")] = "5y",
) -> IndicatorResponse:
    normalized_ticker, normalized_period, history = fetch_stock_history(ticker, period)

    try:
        dataset = calculate_indicators(history)
        rows: list[IndicatorRow] = []
        for timestamp, row in dataset.iterrows():
            indicators = to_indicator_values(timestamp, row)
            rows.append(
                IndicatorRow(
                    **indicators.model_dump(),
                    open=number(row["Open"], "open"),
                    high=number(row["High"], "high"),
                    low=number(row["Low"], "low"),
                    close=number(row["Close"], "close"),
                    volume=int(row["Volume"]),
                )
            )
    except (KeyError, TypeError, ValueError, OverflowError) as exc:
        logger.exception("Indicator calculation failed ticker=%s", normalized_ticker)
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail="Unable to calculate indicators from the returned stock history.",
        ) from exc

    logger.info("Calculated indicators for %d rows ticker=%s", len(rows), normalized_ticker)
    return IndicatorResponse(
        ticker=normalized_ticker,
        period=normalized_period,
        latest=to_indicator_values(dataset.index[-1], dataset.iloc[-1]),
        data=rows,
    )
