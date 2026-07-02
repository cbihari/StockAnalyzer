# Market Overview And News

## Purpose

Provide broad market context and recent ticker news without pretending to be real-time trading infrastructure.

## Business Flow

1. User opens market overview or ticker news.
2. .NET requests normalized market/news data from FastAPI.
3. FastAPI uses the market-data provider boundary and deterministic news labeling.
4. UI shows delayed-data context, breadth, movers, headlines, sentiment/topic/impact labels.

## UI Flow

- Route: `/market`
- Route: `/stocks/:ticker/news`
- Stock detail may preview or link to news.

## Backend Flow

- .NET: `StocksController.GetMarketOverview`, `StocksController.GetNews`.
- FastAPI: `market_overview.py`, `news_sentiment.py`.

## API Endpoints

- `GET /api/stocks/market/overview?region=india`
- `GET /api/stocks/{ticker}/news?lookbackDays=7&limit=10`
- FastAPI: `GET /overview?region=india`
- FastAPI: `GET /news?ticker=AAPL&lookback_days=7&limit=10`

## Models

- `MarketOverviewDto`, `MarketInstrumentDto`, `MarketBreadthDto`
- `StockNewsDto`, `NewsArticleDto`
- Frontend: `MarketOverview`, `StockNews`, `NewsArticle`

## Validation

- Supported regions are India and US.
- News lookback/limit should remain bounded.
- Ticker validation follows shared ticker rules.

## Database Tables

None directly. Data is fetched through provider adapters and returned through APIs.

## Error Handling

- Provider failures surface as API errors.
- UI must show empty/no-relevant-news states instead of unrelated filler.

## Future Improvements

- Add licensed news provider.
- Add relevance scoring and stronger ticker/company disambiguation.
- Persist market snapshots for audit or caching.
