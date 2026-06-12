from datetime import UTC, datetime
from typing import Annotated

from fastapi import APIRouter, HTTPException, Query
from pydantic import BaseModel

from app.market_data_provider import MARKET_DATA_PROVIDER
from app.market_overview import MarketInstrument, _fetch_many
from app.model_registry import normalize_ticker

router = APIRouter(prefix="/stocks", tags=["stocks"])


class StockQuotesResponse(BaseModel):
    as_of: str
    data_source: str
    quotes: list[MarketInstrument]


@router.get("/quotes", response_model=StockQuotesResponse)
def get_stock_quotes(
    tickers: Annotated[str, Query(min_length=1, description="Comma-separated Yahoo Finance tickers")],
) -> StockQuotesResponse:
    symbols = list(dict.fromkeys(
        normalize_ticker(value) for value in tickers.split(",") if value.strip()
    ))
    if not symbols or len(symbols) > 10:
        raise HTTPException(status_code=400, detail="Provide between 1 and 10 unique tickers.")

    quotes = _fetch_many([(symbol, symbol) for symbol in symbols])
    quote_by_symbol = {quote.symbol: quote for quote in quotes}
    ordered = [quote_by_symbol[symbol] for symbol in symbols if symbol in quote_by_symbol]
    if not ordered:
        raise HTTPException(status_code=404, detail="No quote data was found for the requested tickers.")
    return StockQuotesResponse(
        as_of=datetime.now(UTC).isoformat(),
        data_source=MARKET_DATA_PROVIDER.name,
        quotes=ordered,
    )
