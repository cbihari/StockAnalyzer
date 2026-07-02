# State Management

The frontend uses Angular signals for component state and small injectable services for cross-page/browser state. There is no global Redux-style store.

## Route Map

| Route | Component |
| --- | --- |
| `/` | Home |
| `/search` | Stock search |
| `/market` | Market overview |
| `/stocks/:ticker` | Stock detail |
| `/stocks/:ticker/news` | Stock news |
| `/compare` | Stock comparison |
| `/assistant` | AI research assistant |
| `/watchlist` | Watchlist |
| `/portfolio` | Portfolio |
| `/learn`, `/learn/:slug` | Learning center |
| `/notifications` | Notifications |
| `/history` | Prediction history |
| `/accuracy` | Model accuracy |
| `/upgrade` | Upgrade/affiliate page |
| `/login`, `/signup`, `/auth/callback` | Auth |
| `/admin/affiliate-stats` | Affiliate stats |

## Frontend State Services

| Service | Storage | Purpose |
| --- | --- | --- |
| `ClientIdentityService` | `localStorage` | Stable anonymous `X-Client-ID`. |
| `WatchlistService` | `localStorage` plus API sync | Guest watchlist items, tags, notes. |
| `PortfolioService` | `localStorage` plus API sync | Manual holdings. |
| `AlertService` | `localStorage` plus API sync | Price/daily-move rules and notifications. |
| `LearningProgressService` | `localStorage` | Lesson progress. |
| `PredictionHistoryService` | `localStorage` | Legacy/local prediction history cache. |
| `AuthTokenStore` | `sessionStorage` | JWT for current browser session. |
| `AuthService` | signal state plus API | Current user and Google availability. |

## Server State

Remote state is loaded through `StockApiService`. Server-backed workspace endpoints use `X-Client-ID` and optionally authenticated user identity. Logged-in users can claim a guest workspace through `/api/account/claim-workspace`.

## State Rules

- Browser storage is optional; services should fail soft when storage is unavailable.
- Server state should be treated as authoritative after successful sync.
- Keep component state local unless another route needs it.
- Do not store secrets or OpenAI data in browser storage.
