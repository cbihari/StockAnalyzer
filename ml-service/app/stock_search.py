import csv
import logging
from pathlib import Path
from typing import Annotated

from fastapi import APIRouter, Query
from pydantic import BaseModel

from app.market_data_provider import MARKET_DATA_PROVIDER

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/stocks", tags=["stocks"])
STOCK_MASTER_PATH = Path(__file__).resolve().parents[1] / "data" / "stock_master.csv"
MAX_RESULTS = 10


class StockSuggestion(BaseModel):
    symbol: str
    name: str
    exchange: str
    type: str
    country: str


def load_stock_master(path: Path = STOCK_MASTER_PATH) -> list[StockSuggestion]:
    with path.open(newline="", encoding="utf-8") as source:
        return [StockSuggestion(**row) for row in csv.DictReader(source)]


def match_rank(stock: StockSuggestion, query: str) -> tuple[int, str]:
    symbol = stock.symbol.casefold()
    name = stock.name.casefold()
    if symbol.startswith(query):
        rank = 0
    elif name.startswith(query):
        rank = 1
    elif query in symbol:
        rank = 2
    else:
        rank = 3
    return rank, stock.symbol


def local_search(query: str) -> list[StockSuggestion]:
    normalized = query.strip().casefold()
    matches = [
        stock
        for stock in load_stock_master()
        if normalized in stock.symbol.casefold() or normalized in stock.name.casefold()
    ]
    return sorted(matches, key=lambda stock: match_rank(stock, normalized))


def yahoo_search(query: str) -> list[StockSuggestion]:
    try:
        quotes = MARKET_DATA_PROVIDER.search(query, MAX_RESULTS)
    except Exception:
        logger.warning("Yahoo ticker search unavailable query=%s", query, exc_info=True)
        return []

    suggestions: list[StockSuggestion] = []
    for quote in quotes:
        symbol = str(quote.get("symbol") or "").strip().upper()
        name = str(quote.get("longname") or quote.get("shortname") or "").strip()
        if not symbol or not name:
            continue
        suggestions.append(
            StockSuggestion(
                symbol=symbol,
                name=name,
                exchange=str(quote.get("exchDisp") or quote.get("exchange") or "Unknown"),
                type=str(quote.get("typeDisp") or quote.get("quoteType") or "Unknown").title(),
                country=str(quote.get("region") or "Unknown").upper(),
            )
        )
    return suggestions


def search_stocks(query: str) -> list[StockSuggestion]:
    local = local_search(query)
    remote = yahoo_search(query)
    combined: dict[str, StockSuggestion] = {}
    for stock in [*local, *remote]:
        combined.setdefault(stock.symbol, stock)
    normalized = query.strip().casefold()
    matches = [
        stock
        for stock in combined.values()
        if normalized in stock.symbol.casefold() or normalized in stock.name.casefold()
    ]
    return sorted(matches, key=lambda stock: match_rank(stock, normalized))[:MAX_RESULTS]


@router.get("/search", response_model=list[StockSuggestion])
def get_stock_suggestions(
    query: Annotated[str, Query(min_length=2, max_length=100)],
) -> list[StockSuggestion]:
    return search_stocks(query)
