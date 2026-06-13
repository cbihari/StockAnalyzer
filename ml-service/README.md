# StockAnalyzer ML Service

FastAPI service for stock market data and the project's baseline prediction API.

## Endpoints

- `GET /health` - service health check
- `GET /stock/history?ticker=RELIANCE.NS&period=5y` - daily OHLCV history from Yahoo Finance
- `GET /stock/indicators?ticker=RELIANCE.NS&period=5y` - latest indicators and full enriched dataset
- `GET /predict/rule-based?ticker=RELIANCE.NS` - readable rule-based UP/DOWN signal
- `GET /predict/ml?ticker=RELIANCE.NS` - Random Forest prediction using latest indicators
- `POST /models/train?ticker=TCS.NS&period=5y` - train or retrain a ticker model
- `POST /models/train/jobs?ticker=TCS.NS&period=5y` - queue background training
- `GET /models/train/jobs/{job_id}` - inspect training status and metrics
- `GET /models/versions?ticker=TCS.NS` - list immutable model versions
- `GET /stocks/news?ticker=AAPL&lookback_days=7&limit=10` - source-linked headlines with deterministic sentiment and catalyst labels
- `POST /predict` - deterministic placeholder prediction
- `GET /docs` - interactive OpenAPI documentation

The history endpoint returns a JSON array containing:

```json
[
  {
    "date": "2026-06-10",
    "open": 1450.0,
    "high": 1475.0,
    "low": 1440.0,
    "close": 1468.0,
    "volume": 12345678
  }
]
```

Supported periods are `1d`, `5d`, `1mo`, `3mo`, `6mo`, `1y`, `2y`, `5y`, `10y`, `ytd`, and `max`.

The indicators endpoint calculates daily return, SMA 20/50, EMA 20/50, RSI 14,
MACD and signal, Bollinger bands, and volume change with pandas. Values that need
more historical rows than are available are returned as `null`.

## Run locally

From `ml-service/`:

```bash
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
uvicorn app.main:app --reload --host 0.0.0.0 --port 8000
```

Then open `http://localhost:8000/docs` or call:

```bash
curl "http://localhost:8000/stock/history?ticker=RELIANCE.NS&period=5y"
curl "http://localhost:8000/stock/indicators?ticker=RELIANCE.NS&period=5y"
curl "http://localhost:8000/predict/rule-based?ticker=RELIANCE.NS"
curl "http://localhost:8000/predict/ml?ticker=RELIANCE.NS"
curl "http://localhost:8000/stocks/news?ticker=AAPL&lookback_days=7&limit=10"
curl -X POST "http://localhost:8000/models/train?ticker=TCS.NS&period=5y"
```

The news endpoint uses the configured market-data provider, deduplicates recent
headlines, and applies deterministic keyword scoring for sentiment, topic, and
potential impact. Results are cached for 15 minutes. Headline tone is research
context, not a stock-price forecast. No LLM or paid news API is used yet.

`/predict/ml` returns a `rule_based_fallback` response when a model cannot be
trained because the ticker is invalid, fewer than 250 historical rows are
available, OHLCV data is missing, or training fails. If Yahoo Finance also
cannot provide enough data for technical rules, the fallback is neutral at 50%
confidence and leaves `technical_reasons` empty instead of inventing signals.

## Tests

```bash
pytest
```

## Create a training dataset

The target is `1` when the next trading day's close is greater than the current
close, otherwise `0`. Features are calculated using data available through the
current row only; the final row is removed because its future close is unknown.

```bash
python scripts/create_dataset.py --ticker RELIANCE.NS --period 5y
```

The processed CSV is saved to `data/processed/RELIANCE_NS.csv`. Rows with
indicator warm-up values or other nulls are excluded.

## Train the Random Forest model

Training uses the first 80% of the processed rows and tests on the final 20%,
without shuffling. This preserves chronology and prevents future test rows from
entering model fitting.

```bash
python scripts/train_model.py --ticker RELIANCE.NS
```

The command prints accuracy, precision, recall, a confusion matrix in
`[[TN, FP], [FN, TP]]` order, and ranked feature importance. It saves:

- `models/RELIANCE_NS_random_forest.pkl` - fitted `RandomForestClassifier`
- `models/RELIANCE_NS_random_forest_metrics.json` - reproducible evaluation details
- `models/registry/RELIANCE_NS_metadata.json` - searchable model registry metadata
- `models/versions/RELIANCE_NS/{VERSION_ID}/` - immutable model and metric snapshots
- `models/jobs/{JOB_ID}.json` - durable background job status

The model registry centralizes ticker normalization, model existence checks,
model paths, and metadata persistence. Both CLI and automatic training write a
metadata JSON containing training time, row counts, metrics, and feature names.

ML prediction responses include `model_status` (`existing_model` or
`newly_trained_model`), registry `model_accuracy`, and an educational warning.

### RELIANCE.NS baseline results

Using the dataset generated on June 11, 2026, the chronological split contained
947 training rows and 237 test rows. The holdout period was June 24, 2025 through
June 10, 2026.

- Accuracy: `0.5527`
- Precision: `0.5203`
- Recall: `0.5766`
- Confusion matrix: `[[67, 59], [47, 64]]`
- Most important feature: `volume_change` (`0.1099`)

These are classification baseline metrics, not evidence of trading profitability.
The model does not account for transaction costs, slippage, position sizing, or
walk-forward retraining.

## Run with Docker

From the repository root:

```bash
docker compose up --build python-ml-service
```

## Error responses

- `400` - invalid query parameter, including an unsupported period
- `404` - Yahoo Finance returned no history for the ticker
- `502` - Yahoo Finance failed or returned malformed data

Yahoo Finance is an external dependency. Availability, ticker coverage, and data accuracy are not guaranteed.
