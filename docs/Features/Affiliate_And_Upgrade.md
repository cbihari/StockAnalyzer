# Affiliate And Upgrade

## Purpose

Show broker/upgrade partner links, plan status, checkout entry points, and
bounded monetization analytics. This feature is part of the monetization
roadmap, but it must remain clearly separate from stock predictions and AI
research outputs.

See [../15_Business_Monetization_Roadmap.md](../15_Business_Monetization_Roadmap.md)
for the broader packaging and phased rollout plan.

## Business Flow

1. User opens upgrade/partner surface.
2. Frontend loads configured partners.
3. User click is recorded with broker, optional ticker, and anonymous client ID.
4. Quota callout views/clicks, checkout starts, and paid-feature attempts are
   recorded as bounded first-party monetization events.
5. Stats endpoint is available only when explicitly enabled by configuration.

## UI Flow

- Route: `/upgrade`
- Admin route: `/admin/affiliate-stats`
- Admin route: `/admin/monetization-funnel`
- Shared component: `affiliate-note.component.ts`
- API methods: `getAffiliatePartners`, `trackAffiliateClick`,
  `recordMonetizationEvent`, `getAffiliateStats`.

## Backend Flow

- Controller: `AffiliateController`.
- Controller: `MonetizationController`.
- Repository: `AffiliateClickRepository`.
- Repository: `MonetizationEventRepository`.
- Partner config comes from `AFFILIATE_LINKS_CONFIG` JSON or `AffiliatePartners` appsettings.

## API Endpoints

- `GET /api/affiliate/partners`
- `POST /api/affiliate/click`
- `GET /api/affiliate/stats`
- `POST /api/monetization/events`
- `GET /api/monetization/events/funnel`
- `GET /api/monetization/events/funnel/export`
- `POST /api/monetization/checkout`

## Models

- `AffiliatePartnerDto`
- `AffiliateClickRequestDto`
- `AffiliateClickStatDto`
- `MonetizationEventRequestDto`
- `MonetizationFunnelReportDto`
- Domain: `MonetizationEvent`
- Domain: `AffiliateClick`

## Validation

- `X-Client-ID` must be a valid GUID for click tracking.
- Broker must match configured partner.
- Ticker is optional and normalized/truncated.
- Affiliate or sponsored-link disclosure must be visible near partner links.
- Partner links must not be presented as investment advice or as a
  recommendation to trade.
- Monetization events must use approved event names and short metadata only.
- Do not send ticker searches, personal notes, or arbitrary user prompts to
  analytics events.

## Database Tables

- `AffiliateClicks`
- `MonetizationEvents`

## Error Handling

- Malformed partner config returns empty partner list.
- Disabled stats endpoint returns 404.
- Invalid monetization event names or source keys return `ProblemDetails`.
- Disabled monetization funnel reporting returns 404.

## Future Improvements

- Authenticated admin-only stats.
- Real partner URLs and compliance review.
- Click-through disclosure improvements.
- Subscription plans, entitlements, checkout, and webhook processing.
- Plan-aware upgrade prompts for AI explanations, alerts, exports, comparison,
  watchlist size, and portfolio limits.
- Trend charts for monetization funnel reports.
