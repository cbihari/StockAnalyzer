# Stock Analysis

## Purpose

Provide a unified research page for a ticker with quote context, historical prices, indicators, risk signals, prediction summary, news access, and AI explanation entry point.

## Business Flow

1. User searches or enters a ticker.
2. App validates and normalizes the ticker.
3. .NET API requests analysis components from FastAPI and persists relevant data.
4. User reviews educational evidence and warnings before considering any prediction.

## UI Flow

- Route: `/stocks/:ticker`
- Component: `stock-detail.component.ts`
- Uses `StockApiService.getStockAnalysis`, `getHistory`, `getIndicators`, `getMlPrediction`, `getStockNews`, `generateAiExplanation`.
- Shows loading, validation, retraining, fallback, news, AI, and error states.

## Backend Flow

- Controller: `StocksController.GetAnalysis`
- Service: `StockAnalysisService`
- ML client: `MlServiceClient`
- Persistence: stocks, prices, predictions, model metrics as applicable.

## API Endpoints

- `GET /api/stocks/{ticker}/analysis?period=1y`
- `GET /api/stocks/history/{ticker}?period=5y`
- `GET /api/stocks/indicators/{ticker}?period=5y`
- `GET /api/predictions/ml/{ticker}`
- FastAPI: `/stock/history`, `/stock/indicators`, `/predict/ml`

## Models

- Frontend: `StockAnalysis`, `HistoricalPrice`, `IndicatorResponse`, `MlPrediction`
- Backend DTOs: `StockAnalysisDto`, `HistoricalPriceDto`, `IndicatorResponseDto`, `MlPredictionDto`
- Domain: `Stock`, `StockPrice`, `Prediction`, `ModelMetric`

## Validation

- Tickers must be 1-20 uppercase-compatible symbols with letters, numbers, dots, carets, equals signs, or hyphens.
- Period defaults are handled per endpoint.
- Invalid provider data should produce clear 400/404/502-style responses.

## Database Tables

- `Stocks`
- `StockPrices`
- `Predictions`
- `ModelMetrics`

## Error Handling

- .NET maps invalid input to `ProblemDetails`.
- FastAPI validates query parameters and returns provider/training errors.
- UI should distinguish invalid ticker, no data, fallback, timeout, and retraining states.

## Future Improvements

- Stronger company-name-first display across all stock detail panels.
- More explicit delayed-data freshness badges.
- Licensed market-data provider replacement behind the existing ML provider boundary.
