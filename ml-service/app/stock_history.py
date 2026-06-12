import logging
import math
from datetime import date
from typing import Annotated

import pandas as pd
from fastapi import APIRouter, HTTPException, Query, status
from pydantic import BaseModel

from app.market_data_provider import MARKET_DATA_PROVIDER

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/stock", tags=["stock"])

SUPPORTED_PERIODS = {"1d", "5d", "1mo", "3mo", "6mo", "1y", "2y", "5y", "10y", "ytd", "max"}


class HistoricalPrice(BaseModel):
    date: date
    open: float
    high: float
    low: float
    close: float
    volume: int


def number(value: object, field: str) -> float:
    number = float(value)
    if not math.isfinite(number):
        raise ValueError(f"{field} contains a non-finite value")
    return number


def normalize_period(period: str) -> str:
    normalized_period = period.strip().lower()
    if normalized_period not in SUPPORTED_PERIODS:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Unsupported period '{period}'. Supported values: {', '.join(sorted(SUPPORTED_PERIODS))}.",
        )
    return normalized_period


def fetch_stock_history(ticker: str, period: str) -> tuple[str, str, pd.DataFrame]:
    normalized_ticker = ticker.strip().upper()
    normalized_period = normalize_period(period)

    logger.info("Fetching stock history ticker=%s period=%s", normalized_ticker, normalized_period)

    try:
        history = MARKET_DATA_PROVIDER.history(normalized_ticker, normalized_period)
    except Exception as exc:
        logger.exception(
            "Yahoo Finance request failed ticker=%s period=%s",
            normalized_ticker,
            normalized_period,
        )
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail="Unable to fetch stock history from Yahoo Finance.",
        ) from exc

    if history.empty:
        logger.warning(
            "No stock history found ticker=%s period=%s",
            normalized_ticker,
            normalized_period,
        )
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"No historical data found for ticker '{normalized_ticker}'.",
        )

    return normalized_ticker, normalized_period, history


@router.get("/history", response_model=list[HistoricalPrice])
def get_stock_history(
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
) -> list[HistoricalPrice]:
    normalized_ticker, _, history = fetch_stock_history(ticker, period)

    prices: list[HistoricalPrice] = []
    try:
        for timestamp, row in history.iterrows():
            prices.append(
                HistoricalPrice(
                    date=timestamp.date(),
                    open=number(row["Open"], "open"),
                    high=number(row["High"], "high"),
                    low=number(row["Low"], "low"),
                    close=number(row["Close"], "close"),
                    volume=int(row["Volume"]),
                )
            )
    except (KeyError, TypeError, ValueError, OverflowError) as exc:
        logger.exception("Unexpected market data format ticker=%s", normalized_ticker)
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail="Yahoo Finance returned malformed stock history.",
        ) from exc

    logger.info("Fetched %d history rows ticker=%s", len(prices), normalized_ticker)
    return prices
