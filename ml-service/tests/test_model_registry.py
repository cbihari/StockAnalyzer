from datetime import UTC, datetime

import pytest

from app.model_registry import ModelRegistry, normalize_ticker, ticker_key


def test_normalizes_ticker_and_creates_safe_key() -> None:
    assert normalize_ticker(" tcs.ns ") == "TCS.NS"
    assert ticker_key("TCS.NS") == "TCS_NS"


def test_rejects_invalid_ticker() -> None:
    with pytest.raises(ValueError, match="Ticker must contain"):
        normalize_ticker("AAPL/USD")


def test_model_paths_and_existence(tmp_path) -> None:
    registry = ModelRegistry(tmp_path)
    model_path = registry.get_model_path("msft")

    assert model_path == tmp_path / "models" / "MSFT_random_forest.pkl"
    assert registry.model_exists("MSFT") is False

    model_path.parent.mkdir(parents=True)
    model_path.touch()
    assert registry.model_exists("MSFT") is True


def test_saves_and_loads_metadata(tmp_path) -> None:
    registry = ModelRegistry(tmp_path)
    trained_at = datetime(2026, 6, 11, 10, 30, tzinfo=UTC)

    saved = registry.save_model_metadata(
        ticker="tcs.ns",
        model_name="RandomForestClassifier",
        training_rows=1000,
        test_rows=250,
        accuracy=0.56,
        precision=0.58,
        recall=0.54,
        features=["open", "close"],
        trained_at=trained_at,
    )
    loaded = registry.load_model_metadata("TCS.NS")

    assert loaded == saved
    assert saved.model_path == "models/TCS_NS_random_forest.pkl"
    assert saved.trained_at == "2026-06-11T10:30:00+00:00"
    assert registry.get_metadata_path("TCS.NS") == (
        tmp_path / "models" / "registry" / "TCS_NS_metadata.json"
    )


def test_load_returns_none_when_metadata_is_missing(tmp_path) -> None:
    assert ModelRegistry(tmp_path).load_model_metadata("INFY.NS") is None
