# Portfolio

## Purpose

Provide manual portfolio tracking with delayed valuation, gain/loss, allocation, daily movement, and concentration context.

## Business Flow

1. User enters holdings manually.
2. Holdings are saved locally and synced to the guest/auth workspace.
3. Backend loads holdings and quote snapshots.
4. Portfolio service calculates summary, allocation, and risk/concentration context.

## UI Flow

- Route: `/portfolio`
- Service: `PortfolioService`
- API calls: `getWorkspacePortfolio`, `saveWorkspacePortfolio`, `getPortfolioSummary`.

## Backend Flow

- `WorkspaceController` stores raw holdings.
- `PortfolioController.GetSummary` returns calculated summary.
- `PortfolioService` orchestrates workspace holdings and stock quotes.
- `GuestWorkspaceService` checks the `portfolio_holding` stored limit before
  saving workspace JSON.

## API Endpoints

- `GET /api/workspace/portfolio`
- `PUT /api/workspace/portfolio`
- `GET /api/portfolio/summary`

## Models

- `PortfolioHoldingDto`
- `PortfolioHoldingSummaryDto`
- `PortfolioCurrencyBucketDto`
- `PortfolioSummaryDto`
- Frontend: `PortfolioHolding`, `PortfolioSummary`

## Validation

- `X-Client-ID` is required.
- Holdings should have valid ticker, quantity, average cost, and currency assumptions.
- Portfolio size is bounded by the active plan's `portfolio_holding` stored
  limit.

## Database Tables

- `GuestWorkspaces.PortfolioJson`

## Error Handling

- Invalid holdings should return `ProblemDetails`.
- Quote/provider failures should show summary error without deleting saved holdings.
- Plan-limit failures return `402`; the frontend rolls back to the last synced
  portfolio state and links users to `/upgrade`.

## Future Improvements

- Import/export holdings.
- Multi-currency cost basis with explicit exchange-rate source.
- Account-level portfolio history.
