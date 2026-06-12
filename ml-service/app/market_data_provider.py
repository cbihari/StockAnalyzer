import os
from abc import ABC, abstractmethod

import pandas as pd
import yfinance as yf


class MarketDataProvider(ABC):
    name: str

    @abstractmethod
    def history(self, ticker: str, period: str) -> pd.DataFrame:
        raise NotImplementedError

    @abstractmethod
    def search(self, query: str, limit: int) -> list[dict[str, object]]:
        raise NotImplementedError


class YahooFinanceProvider(MarketDataProvider):
    name = "yahoo_finance"

    def history(self, ticker: str, period: str) -> pd.DataFrame:
        return yf.Ticker(ticker).history(
            period=period,
            interval="1d",
            auto_adjust=False,
            actions=False,
            raise_errors=True,
        )

    def search(self, query: str, limit: int) -> list[dict[str, object]]:
        return yf.Search(
            query,
            max_results=limit,
            news_count=0,
            lists_count=0,
            include_cb=False,
            include_nav_links=False,
            include_research=False,
            include_cultural_assets=False,
            timeout=5,
        ).quotes


def get_market_data_provider() -> MarketDataProvider:
    provider_name = os.getenv("MARKET_DATA_PROVIDER", "yahoo").strip().lower()
    if provider_name == "yahoo":
        return YahooFinanceProvider()
    raise RuntimeError(
        f"Unsupported MARKET_DATA_PROVIDER '{provider_name}'. Configure a registered provider adapter."
    )


MARKET_DATA_PROVIDER = get_market_data_provider()
