# StockAnalyzer

StockAnalyzer is an educational full-stack stock-analysis dashboard. Enter any
valid Yahoo Finance ticker to explore historical prices and technical indicators
and estimate whether the next trading-day close may be **UP** or **DOWN**.

Each ticker receives its own Random Forest model. If no model exists,
StockAnalyzer creates a leakage-safe dataset, trains the model, registers its
metrics, and returns the prediction in the same request. If training is not
possible, the application returns a clearly labeled rule-based fallback.

> **Disclaimer:** StockAnalyzer is for educational purposes only. It is not
> financial advice, a trading recommendation, or a promise of future results.

## Screenshots

Add or replace screenshots under `docs/screenshots/` as the interface evolves.

### Dashboard

![StockAnalyzer dashboard](docs/screenshots/dashboard.png)

### Stock analysis

![RELIANCE.NS stock detail](docs/screenshots/stock-detail.png)

Suggested additional captures:

- Model Accuracy page with ticker-specific metrics
- Background model retraining with durable job status and immutable versions
- Guest watchlist with batch delayed quotes, notes, tags, daily change, range, and sparklines
- Learning Center with RSI, MACD, EMA, SMA, volume, and risk foundations
- Contextual indicator lessons, knowledge checks, glossary, and guest progress
- Browser-local price and daily-move alerts with an in-app notification center
- First-time model training progress
- Rule-Based Fallback badge and warning

## Features

- Angular dashboard with loading, validation, retry, and error states
- Research-first stock brief with quote, directional estimate, visible risk, and supporting/conflicting evidence
- Two-to-three stock comparison with normalized cross-currency performance
- India and US market overview with major indices and transparent sampled movers/breadth
- Debounced ticker and company autocomplete with keyboard navigation
- Historical OHLCV data and Chart.js closing-price chart
- SMA, EMA, RSI, MACD, Bollinger Bands, returns, and volume indicators
- Rule-based and Random Forest UP/DOWN predictions
- Dynamic Yahoo Finance ticker support with one saved ML model per ticker
- Automatic first-time training and manual **Retrain Model** workflow
- Ticker-specific accuracy, precision, recall, confusion matrix, and row counts
- Rule-based fallback for invalid, incomplete, or insufficient market data
- Deterministic beginner explanation without paid LLM APIs
- Prediction outcome evaluation and accuracy tracking
- PostgreSQL-backed prediction accountability history with filters, outcome review, and CSV export
- PostgreSQL persistence through Entity Framework Core migrations
- Structured request logs with correlation IDs
- Swagger/OpenAPI documentation for .NET and FastAPI
- Angular, .NET, and Python unit tests with GitHub Actions CI

## Architecture

| Service | Technology | Host URL |
| --- | --- | --- |
| `angular-frontend` | Angular 20, Nginx, Chart.js | `http://localhost:4200` |
| `dotnet-backend` | .NET 9 Web API, EF Core | `http://localhost:8080` |
| `python-ml-service` | FastAPI, pandas, scikit-learn | `http://localhost:8000` |
| `postgres` | PostgreSQL 17 | `localhost:5433` |

The browser calls the .NET API. The .NET API calls FastAPI and stores stocks,
prices, predictions, outcomes, and model metrics in PostgreSQL.

Market data is accessed through a provider interface in the ML service. The
default adapter uses Yahoo Finance, while the application and API contracts are
kept provider-neutral so a licensed feed can replace it without rewriting the
analysis, model, backend, or frontend layers.

## Quick Start

Requirements: Docker Desktop with Docker Compose.

```bash
cp .env.example .env
docker compose up --build
```

Open `http://localhost:4200`. When a ticker has no saved model, the ML service downloads up
to five years of history, creates a leakage-safe dataset, trains a Random Forest,
and saves the model before returning the prediction. Generated files persist in
`ml-service/models/` and `ml-service/data/processed/`. Each model also receives
a registry entry under `ml-service/models/registry/` with its ticker, path,
training timestamp, row counts, metrics, and features.

### Sample Tickers

Indian NSE tickers:

```text
RELIANCE.NS  TCS.NS  INFY.NS  HDFCBANK.NS
```

US tickers:

```text
AAPL  MSFT  TSLA  NVDA
```

Stop the stack without deleting database data:

```bash
docker compose down
```

Delete containers and the PostgreSQL volume:

```bash
docker compose down -v
```

## Environment

Compose reads values from `.env`. Never commit real credentials.

| Variable | Default example | Purpose |
| --- | --- | --- |
| `POSTGRES_DB` | `stock_analyzer` | Database name |
| `POSTGRES_USER` | `stock_user` | Database user |
| `POSTGRES_PASSWORD` | `change_me` | Database password |
| `POSTGRES_PORT` | `5433` | PostgreSQL host port |
| `FRONTEND_PORT` | `4200` | Angular host port |
| `BACKEND_PORT` | `8080` | .NET host port |
| `ML_SERVICE_PORT` | `8000` | FastAPI host port |
| `ASPNETCORE_ENVIRONMENT` | `Development` | .NET environment |
| `FRONTEND_URL` | `http://localhost:4200` | Allowed CORS origin |
| `ML_LOG_LEVEL` | `INFO` | FastAPI logging level |
| `MARKET_DATA_PROVIDER` | `yahoo` | ML market-data adapter selection |
| `MARKET_DATA_PROVIDER_NAME` | `yahoo_finance` | Provider label returned by the .NET research API |

## API Documentation

- .NET Swagger UI: `http://localhost:8080/swagger`
- .NET OpenAPI JSON: `http://localhost:8080/swagger/v1/swagger.json`
- FastAPI Swagger UI: `http://localhost:8000/docs`
- FastAPI OpenAPI JSON: `http://localhost:8000/openapi.json`

Useful requests:

```bash
curl "http://localhost:8080/api/stocks/history/RELIANCE.NS?period=1y"
curl "http://localhost:8080/api/stocks/indicators/RELIANCE.NS?period=1y"
curl "http://localhost:8080/api/stocks/search?query=rel"
curl "http://localhost:8080/api/stocks/AAPL/analysis?period=1y"
curl "http://localhost:8080/api/stocks/compare?tickers=AAPL,MSFT&period=1y"
curl "http://localhost:8080/api/stocks/market/overview?region=india"
curl "http://localhost:8080/api/predictions/ml/RELIANCE.NS"
curl "http://localhost:8080/api/predictions/history?ticker=AAPL&outcome=pending"
curl "http://localhost:8080/api/predictions/explain/RELIANCE.NS"
curl -X POST "http://localhost:8080/api/predictions/evaluate"
curl "http://localhost:8080/api/model/accuracy"
curl "http://localhost:8080/api/model/metrics/TCS.NS"
curl -X POST "http://localhost:8080/api/model/train/TCS.NS?period=5y"
```

## Health And Logs

```bash
curl http://localhost:4200/health
curl http://localhost:8080/api/health
curl http://localhost:8000/health
docker compose ps
docker compose logs -f dotnet-backend python-ml-service
```

API responses include `X-Correlation-ID`. .NET errors use RFC-style problem
details, while unexpected FastAPI errors return a generic message and a
correlation ID. Logs contain request method, path, status, duration, and ID.

## Tests

Frontend:

```bash
cd frontend
npm ci
npm test -- --watch=false --browsers=ChromeHeadless
npm run build
```

Backend:

```bash
dotnet test tests/StockAnalyzer.UnitTests/StockAnalyzer.UnitTests.csproj
dotnet build backend/StockAnalyzer.Api.csproj
```

ML service:

```bash
cd ml-service
python3 -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
pytest -q
```

## Model Training

The easiest option is the **Retrain Model** button on the Stock Detail page.
It trains with five years of data, stores the new metrics, and refreshes the
prediction automatically.

The Model Accuracy page also supports non-blocking retraining. It queues a
background job, shows live status, and refreshes the active metrics when the
job succeeds. Each successful run is retained under
`ml-service/models/versions/{TICKER_KEY}/{VERSION_ID}/`; the current model path
remains unchanged for prediction compatibility. Job records are persisted in
`ml-service/models/jobs/`. Set `TRAINING_WORKERS` to control local concurrency.

Through the .NET gateway:

```bash
curl -X POST "http://localhost:8080/api/model/train/TCS.NS?period=5y"

# Background workflow
curl -X POST "http://localhost:8080/api/model/train/TCS.NS/jobs?period=5y"
curl "http://localhost:8080/api/model/train/jobs/{job-id}"
curl "http://localhost:8080/api/model/versions/TCS.NS"
```

Directly from `ml-service/`:

From `ml-service/`:

```bash
python scripts/create_dataset.py --ticker RELIANCE.NS --period 5y
python scripts/train_model.py --ticker RELIANCE.NS
```

Training uses the first 80% of chronological rows and tests on the final 20%
without shuffling. Models are saved under `ml-service/models/`.

The prediction endpoint performs these steps automatically when a requested
ticker has no model. Training requires at least 250 historical rows and complete
OHLCV data. Invalid tickers, insufficient data, missing OHLCV values, and model
training failures return a clearly labeled rule-based fallback instead of
breaking the prediction flow.

## Watchlist

Open any Stock Detail page and select **Add to watchlist**, then use the
**Watchlist** navigation item for a compact daily snapshot. Guest watchlists
are stored in the current browser and capped at ten tickers. Each item supports
a 500-character research note and up to five normalized tags, with tag-based
filtering on the watchlist page. Existing ticker-only browser lists migrate
automatically to the structured item format. Quote retrieval is
already a batch backend contract (`GET /api/stocks/quotes?tickers=AAPL,MSFT`),
so browser storage can later be replaced by authenticated account persistence
without coupling the UI to Yahoo Finance.

## Learning Center

Open **Learn** from the main navigation for six short foundational lessons,
common mistakes, practical examples, one-question knowledge checks, and a
research glossary. Lesson completion is stored in the current browser. Stock
Detail indicator cards link directly to the relevant lesson, keeping education
inside the research workflow. Learning content lives in a separate typed
content module so it can later be supplied by a CMS without rebuilding the UI.

## Alerts

Watchlist items support price-above, price-below, and absolute daily-move
alerts. Rules include once/daily frequency, a 6-hour to 3-day cooldown, quiet
hours, and in-app delivery. The notification center records both trigger time
and delayed market-data timestamp and links back to the stock evidence.

The current guest implementation evaluates alerts whenever this browser loads
fresh watchlist quotes. It does not claim real-time monitoring. The alert rule
and notification models are isolated so a scheduled backend worker and email
or push providers can replace browser evaluation later.

## Limitations

- Yahoo Finance is an unofficial external data dependency and can be delayed,
  incomplete, rate-limited, or unavailable.
- The model uses a simple Random Forest baseline and technical-price features;
  it does not use news, fundamentals, macroeconomic data, or market sentiment.
- Metrics use one chronological 80/20 split, not walk-forward validation.
- A higher accuracy score does not imply profitability or account for fees,
  slippage, liquidity, taxes, or risk management.
- First-time analysis can take longer because a separate model is trained for
  each ticker.

## Database

The .NET backend applies EF Core migrations on startup. The schema contains
`Stocks`, `StockPrices`, `Predictions`, and `ModelMetrics`. PostgreSQL data is
stored in the named Docker volume `postgres_data`.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) before opening a pull request. CI runs
frontend tests/build, backend tests/build, Python tests, and Docker image builds.

## Educational Disclaimer

Yahoo Finance is an external dependency, so availability, ticker coverage, and
data accuracy are not guaranteed. Model accuracy does not represent trading
profitability and excludes fees, slippage, liquidity, position sizing, and
walk-forward retraining.

**StockAnalyzer is for educational and experimental purposes only. It is not
financial advice, investment advice, or a recommendation to buy or sell any
security.**
