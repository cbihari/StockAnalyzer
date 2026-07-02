from fastapi.testclient import TestClient

from app.main import app


def test_health_endpoint_reports_service_and_provider() -> None:
    response = TestClient(app).get("/health")

    assert response.status_code == 200
    payload = response.json()
    assert payload["status"] == "healthy"
    assert payload["service"] == "stock-analyzer-ml"
    assert payload["market_data_provider"] == "yahoo_finance"
    assert payload["timestamp"].endswith("+00:00")


def test_prediction_request_normalizes_ticker_without_external_calls() -> None:
    response = TestClient(app).post("/predict", json={"symbol": "aapl"})

    assert response.status_code == 200
    assert response.json()["symbol"] == "AAPL"


def test_prediction_request_rejects_invalid_ticker() -> None:
    response = TestClient(app).post("/predict", json={"symbol": "AAPL/USD"})

    assert response.status_code == 422
