# Affiliate And Upgrade

## Purpose

Show broker/upgrade partner links and optionally track aggregate click statistics.
This feature is part of the monetization roadmap, but it must remain clearly
separate from stock predictions and AI research outputs.

See [../15_Business_Monetization_Roadmap.md](../15_Business_Monetization_Roadmap.md)
for the broader packaging and phased rollout plan.

## Business Flow

1. User opens upgrade/partner surface.
2. Frontend loads configured partners.
3. User click is recorded with broker, optional ticker, and anonymous client ID.
4. Stats endpoint is available only when explicitly enabled by configuration.

## UI Flow

- Route: `/upgrade`
- Admin route: `/admin/affiliate-stats`
- Shared component: `affiliate-note.component.ts`
- API methods: `getAffiliatePartners`, `trackAffiliateClick`, `getAffiliateStats`.

## Backend Flow

- Controller: `AffiliateController`.
- Repository: `AffiliateClickRepository`.
- Partner config comes from `AFFILIATE_LINKS_CONFIG` JSON or `AffiliatePartners` appsettings.

## API Endpoints

- `GET /api/affiliate/partners`
- `POST /api/affiliate/click`
- `GET /api/affiliate/stats`

## Models

- `AffiliatePartnerDto`
- `AffiliateClickRequestDto`
- `AffiliateClickStatDto`
- Domain: `AffiliateClick`

## Validation

- `X-Client-ID` must be a valid GUID for click tracking.
- Broker must match configured partner.
- Ticker is optional and normalized/truncated.
- Affiliate or sponsored-link disclosure must be visible near partner links.
- Partner links must not be presented as investment advice or as a
  recommendation to trade.

## Database Tables

- `AffiliateClicks`

## Error Handling

- Malformed partner config returns empty partner list.
- Disabled stats endpoint returns 404.

## Future Improvements

- Authenticated admin-only stats.
- Real partner URLs and compliance review.
- Click-through disclosure improvements.
- Subscription plans, entitlements, checkout, and webhook processing.
- Plan-aware upgrade prompts for AI explanations, alerts, exports, comparison,
  watchlist size, and portfolio limits.
