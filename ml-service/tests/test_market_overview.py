import pandas as pd

from app import market_overview


def test_market_overview_ranks_breadth_and_movers(monkeypatch) -> None:
    def fake_history(symbol: str, period: str) -> pd.DataFrame:
        changes = {"RELIANCE.NS": (100, 110, 1000), "TCS.NS": (100, 90, 500)}
        before, after, volume = changes.get(symbol, (100, 102, 100))
        return pd.DataFrame({"Close": [before, after], "High": [before + 1, after + 1], "Low": [before - 1, after - 1], "Volume": [50, volume]})

    monkeypatch.setattr(market_overview.MARKET_DATA_PROVIDER, "history", fake_history)
    monkeypatch.setitem(market_overview.REGIONS, "india", {
        "indices": [("^NSEI", "NIFTY 50")],
        "universe": [("RELIANCE.NS", "Reliance Industries"), ("TCS.NS", "Tata Consultancy Services")],
    })

    result = market_overview.build_market_overview("india")

    assert result.breadth.advancers == 1
    assert result.breadth.decliners == 1
    assert result.breadth.sentiment == "MIXED"
    assert result.top_gainers[0].symbol == "RELIANCE.NS"
    assert result.top_losers[0].symbol == "TCS.NS"
    assert result.most_active[0].symbol == "RELIANCE.NS"
