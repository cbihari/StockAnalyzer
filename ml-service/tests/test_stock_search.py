from app import stock_search
from app.stock_search import StockSuggestion


def test_local_search_prioritizes_symbol_then_name_matches(monkeypatch) -> None:
    stocks = [
        StockSuggestion(symbol="XREL", name="Other", exchange="NSE", type="Equity", country="India"),
        StockSuggestion(symbol="ABC", name="Reliance Example", exchange="NSE", type="Equity", country="India"),
        StockSuggestion(symbol="RELIANCE.NS", name="Reliance Industries", exchange="NSE", type="Equity", country="India"),
    ]
    monkeypatch.setattr(stock_search, "load_stock_master", lambda: stocks)

    result = stock_search.local_search("rel")

    assert [stock.symbol for stock in result] == ["RELIANCE.NS", "ABC", "XREL"]


def test_search_uses_local_results_when_yahoo_is_unavailable(monkeypatch) -> None:
    monkeypatch.setattr(stock_search, "yahoo_search", lambda query: [])

    result = stock_search.search_stocks("rel")

    assert [stock.symbol for stock in result[:3]] == [
        "RELCAPITAL.NS",
        "RELIANCE.BO",
        "RELIANCE.NS",
    ]


def test_search_deduplicates_and_limits_results(monkeypatch) -> None:
    local = [
        StockSuggestion(symbol=f"AA{index}", name=f"Company {index}", exchange="NSE", type="Equity", country="India")
        for index in range(12)
    ]
    monkeypatch.setattr(stock_search, "local_search", lambda query: local)
    monkeypatch.setattr(stock_search, "yahoo_search", lambda query: [local[0]])

    result = stock_search.search_stocks("aa")

    assert len(result) == 10
    assert len({stock.symbol for stock in result}) == 10
