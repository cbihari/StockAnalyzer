# Watchlist Alerts And Notifications

## Purpose

Let users save tickers, organize them with notes/tags, configure simple browser-local alert rules, and view generated in-app notifications.

## Business Flow

1. Guest browser receives or reuses an anonymous `X-Client-ID`.
2. User adds watchlist items and alert rules.
3. Frontend stores state locally and syncs to `/api/workspace/*`.
4. Quote refreshes can generate notification items.

## UI Flow

- Routes: `/watchlist`, `/notifications`.
- Services: `WatchlistService`, `AlertService`, `ClientIdentityService`.
- API wrapper: `StockApiService.getWorkspaceWatchlist`, `saveWorkspaceWatchlist`, `getWorkspaceAlerts`, `saveWorkspaceAlerts`, `getStockQuotes`.

## Backend Flow

- Controller: `WorkspaceController`.
- Service: `GuestWorkspaceService`.
- Repository: `GuestWorkspaceRepository`.
- Monetization: `GuestWorkspaceService` checks `watchlist_item` and `alert_rule`
  stored limits before saving workspace JSON.

## API Endpoints

- `GET /api/workspace/watchlist`
- `PUT /api/workspace/watchlist`
- `GET /api/workspace/alerts`
- `PUT /api/workspace/alerts`
- `GET /api/stocks/quotes?tickers=...`

## Models

- `WorkspaceWatchlistItemDto`
- `WorkspaceAlertStateDto`, `WorkspaceAlertRuleDto`, `WorkspaceNotificationDto`
- Frontend: `WatchlistItem`, `AlertRule`, `AlertNotification`

## Validation

- `X-Client-ID` must be a valid GUID for anonymous workspace sync.
- Ticker, notes, tags, alert thresholds, and list sizes should remain bounded.
- Watchlist size is limited by the active plan's `watchlist_item` stored limit.
- Alert-rule count is limited by the active plan's `alert_rule` stored limit.

## Database Tables

- `GuestWorkspaces` JSONB columns: `WatchlistJson`, `AlertStateJson`.

## Error Handling

- Browser storage failures should degrade to in-memory state.
- API sync failures should keep local state usable and show a non-destructive error.
- Plan-limit failures return `402`; the frontend rolls back to the last synced
  workspace state and shows a `/upgrade` callout.

## Future Improvements

- Server-side scheduled alert evaluation.
- Push/email notifications for authenticated users.
- Shared watchlists.
