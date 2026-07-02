import pandas as pd

from app import market_data_provider


def test_yahoo_history_uses_expected_options(monkeypatch) -> None:
    calls: dict[str, object] = {}
    expected = pd.DataFrame({"Close": [100.0]})

    class FakeTicker:
        def history(self, **kwargs):
            calls.update(kwargs)
            return expected

    monkeypatch.setattr(market_data_provider.yf, "Ticker", lambda ticker: FakeTicker())

    result = market_data_provider.YahooFinanceProvider().history("AAPL", "5y")

    assert result is expected
    assert calls == {
        "period": "5y",
        "interval": "1d",
        "auto_adjust": False,
        "actions": False,
        "raise_errors": True,
    }
