# Search And Autocomplete

## Purpose

Help users find stocks by ticker or company name, reducing reliance on knowing exact Yahoo Finance symbols.

## Business Flow

1. User types a query.
2. Frontend debounces and calls search API.
3. FastAPI searches `stock_master.csv`.
4. User selects a result and navigates to stock detail.

## UI Flow

- Routes/components: `/search`, `stock-search.component.ts`, `ticker-autocomplete.component.ts`.
- Keyboard navigation and loading/open state are handled by the shared autocomplete component.

## Backend Flow

- .NET: `StocksController.Search` -> `StockAnalysisService.SearchStocksAsync`.
- FastAPI: `/stocks/search` in `stock_search.py`.

## API Endpoints

- `GET /api/stocks/search?query=rel`
- FastAPI: `GET /stocks/search?query=rel`

## Models

- Frontend: `StockSuggestion`
- Backend: `StockSuggestionDto`
- FastAPI: `StockSuggestion`

## Validation

- Query is required and should be short enough for interactive search.
- Results should normalize tickers while preserving display company names.

## Database Tables

None directly. Search is backed by `ml-service/data/stock_master.csv`.

## Error Handling

- Empty/invalid query should show a helpful validation state.
- Provider/data file failures should not crash navigation.

## Future Improvements

- Expand Indian company aliases and common names.
- Rank exact company-name matches above symbol-only matches for beginners.
