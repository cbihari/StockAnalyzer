import logging
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import UTC, datetime
from typing import Literal

import pandas as pd
from fastapi import APIRouter, HTTPException, Query
from pydantic import BaseModel

from app.market_data_provider import MARKET_DATA_PROVIDER

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/market", tags=["market"])

REGIONS = {
    "india": {
        "indices": [("^NSEI", "NIFTY 50"), ("^NSEBANK", "BANK NIFTY"), ("^BSESN", "SENSEX")],
        "universe": [("RELIANCE.NS", "Reliance Industries"), ("TCS.NS", "Tata Consultancy Services"), ("INFY.NS", "Infosys"), ("HDFCBANK.NS", "HDFC Bank"), ("ICICIBANK.NS", "ICICI Bank"), ("SBIN.NS", "State Bank of India")],
    },
    "us": {
        "indices": [("^GSPC", "S&P 500"), ("^IXIC", "NASDAQ Composite"), ("^DJI", "Dow Jones")],
        "universe": [("AAPL", "Apple"), ("MSFT", "Microsoft"), ("NVDA", "NVIDIA"), ("TSLA", "Tesla"), ("AMZN", "Amazon"), ("GOOGL", "Alphabet"), ("META", "Meta Platforms"), ("AMD", "Advanced Micro Devices")],
    },
}


class MarketInstrument(BaseModel):
    symbol: str
    name: str
    price: float
    change: float
    change_percent: float
    day_high: float
    day_low: float
    volume: int
    sparkline: list[float]


class MarketBreadth(BaseModel):
    advancers: int
    decliners: int
    unchanged: int
    sentiment: Literal["POSITIVE", "MIXED", "NEGATIVE"]
    coverage: int


class MarketOverview(BaseModel):
    region: str
    session_status: Literal["DELAYED_OR_CLOSED"]
    as_of: str
    data_source: str
    coverage_note: str
    indices: list[MarketInstrument]
    breadth: MarketBreadth
    top_gainers: list[MarketInstrument]
    top_losers: list[MarketInstrument]
    most_active: list[MarketInstrument]
    insights: list[str]


def _instrument(symbol: str, name: str | None = None) -> MarketInstrument | None:
    frame = MARKET_DATA_PROVIDER.history(symbol, "1mo")
    if frame.empty or len(frame) < 2 or "Close" not in frame.columns:
        return None
    valid = frame.dropna(subset=["Close"])
    if len(valid) < 2:
        return None
    latest = valid.iloc[-1]
    previous = valid.iloc[-2]
    close = float(latest["Close"])
    previous_close = float(previous["Close"])
    change = close - previous_close
    return MarketInstrument(
        symbol=symbol,
        name=name or symbol,
        price=close,
        change=change,
        change_percent=0 if previous_close == 0 else change / previous_close,
        day_high=float(latest.get("High", close)),
        day_low=float(latest.get("Low", close)),
        volume=int(latest.get("Volume", 0) or 0),
        sparkline=[round(float(value), 4) for value in valid["Close"].tail(20).tolist()],
    )


def _fetch_many(symbols: list[tuple[str, str | None]]) -> list[MarketInstrument]:
    results: list[MarketInstrument] = []
    with ThreadPoolExecutor(max_workers=min(6, len(symbols))) as executor:
        futures = {executor.submit(_instrument, symbol, name): symbol for symbol, name in symbols}
        for future in as_completed(futures):
            try:
                instrument = future.result()
                if instrument:
                    results.append(instrument)
            except Exception as exc:
                logger.warning("Market snapshot failed for %s: %s", futures[future], exc)
    return results


def build_market_overview(region: str) -> MarketOverview:
    config = REGIONS[region]
    indices = _fetch_many([(symbol, name) for symbol, name in config["indices"]])
    universe = _fetch_many(config["universe"])
    if not indices and not universe:
        raise HTTPException(status_code=502, detail="Market data is temporarily unavailable.")

    advancers = sum(item.change > 0 for item in universe)
    decliners = sum(item.change < 0 for item in universe)
    unchanged = len(universe) - advancers - decliners
    sentiment = "POSITIVE" if advancers > decliners * 1.3 else "NEGATIVE" if decliners > advancers * 1.3 else "MIXED"
    gainers = sorted((item for item in universe if item.change > 0), key=lambda item: item.change_percent, reverse=True)[:5]
    losers = sorted((item for item in universe if item.change < 0), key=lambda item: item.change_percent)[:5]
    active = sorted(universe, key=lambda item: item.volume, reverse=True)[:5]
    insights = [
        f"{advancers} of {len(universe)} tracked liquid stocks are advancing." if universe else "Breadth data is unavailable.",
        f"Breadth is {sentiment.lower()} across the configured {region.title()} universe.",
    ]
    if gainers and losers:
        insights.append(
            f"The spread between the strongest and weakest tracked stock is {(gainers[0].change_percent - losers[0].change_percent):.1%}."
        )
    elif gainers:
        insights.append("No declining stock was found in the configured sample for the latest session.")
    elif losers:
        insights.append("No advancing stock was found in the configured sample for the latest session.")

    return MarketOverview(
        region=region,
        session_status="DELAYED_OR_CLOSED",
        as_of=datetime.now(UTC).isoformat(),
        data_source=MARKET_DATA_PROVIDER.name,
        coverage_note="Movers and breadth use a configurable liquid-stock sample, not the full exchange.",
        indices=sorted(indices, key=lambda item: [symbol for symbol, _ in config["indices"]].index(item.symbol)),
        breadth=MarketBreadth(advancers=advancers, decliners=decliners, unchanged=unchanged, sentiment=sentiment, coverage=len(universe)),
        top_gainers=gainers,
        top_losers=losers,
        most_active=active,
        insights=insights,
    )


@router.get("/overview", response_model=MarketOverview)
def get_market_overview(region: str = Query(default="india", pattern="^(india|us)$")) -> MarketOverview:
    return build_market_overview(region)
