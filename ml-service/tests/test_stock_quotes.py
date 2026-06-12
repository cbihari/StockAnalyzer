from app import stock_quotes
from app.market_overview import MarketInstrument


def quote(symbol: str) -> MarketInstrument:
    return MarketInstrument(
        symbol=symbol,
        name=symbol,
        price=100,
        change=1,
        change_percent=0.01,
        day_high=101,
        day_low=98,
        volume=1000,
        sparkline=[98, 99, 100],
    )


def test_quotes_are_normalized_deduplicated_and_ordered(monkeypatch) -> None:
    monkeypatch.setattr(
        stock_quotes,
        "_fetch_many",
        lambda symbols: [quote("MSFT"), quote("AAPL")],
    )

    response = stock_quotes.get_stock_quotes(" aapl,MSFT,aapl ")

    assert [item.symbol for item in response.quotes] == ["AAPL", "MSFT"]
