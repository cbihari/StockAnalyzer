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
5. Upgrade checkout uses Razorpay Test Mode order checkout through
   `POST /api/payments/create-order` and `POST /api/payments/verify`. The older
   `IPaymentProvider` path remains available for manual local checkout and
   Razorpay Payment Links/webhooks.
6. Stats endpoint is available only when explicitly enabled by configuration.

## UI Flow

- Route: `/upgrade`
- Admin route: `/admin/affiliate-stats`
- Admin route: `/admin/monetization-funnel`
- Shared component: `affiliate-note.component.ts`
- API methods: `getAffiliatePartners`, `trackAffiliateClick`,
  `recordMonetizationEvent`, `getAffiliateStats`, `createRazorpayOrder`,
  `verifyRazorpayPayment`.

## Backend Flow

- Controller: `AffiliateController`.
- Controller: `MonetizationController`.
- Controller: `PaymentsController`.
- Repository: `AffiliateClickRepository`.
- Repository: `MonetizationEventRepository`.
- Payment services: `RazorpayCheckoutService`, `ManualPaymentProvider`, and
  `RazorpayPaymentProvider`.
- Partner config comes from `AFFILIATE_LINKS_CONFIG` JSON or `AffiliatePartners` appsettings.

## API Endpoints

- `GET /api/affiliate/partners`
- `POST /api/affiliate/click`
- `GET /api/affiliate/stats`
- `POST /api/monetization/events`
- `GET /api/monetization/events/funnel`
- `GET /api/monetization/events/funnel/export`
- `POST /api/monetization/checkout`
- `POST /api/monetization/webhooks/{provider}`
- `POST /api/payments/create-order`
- `POST /api/payments/verify`

## Models

- `AffiliatePartnerDto`
- `AffiliateClickRequestDto`
- `AffiliateClickStatDto`
- `MonetizationEventRequestDto`
- `MonetizationFunnelReportDto`
- `MonetizationFunnelDailyDto`
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
- Razorpay webhooks with missing or invalid `X-Razorpay-Signature` are rejected.
- Razorpay Checkout verification rejects missing or invalid payment/order
  signatures, non-captured payments, amount/currency mismatches, and non-Test
  Mode keys.

## Future Improvements

- Authenticated admin-only stats.
- Real partner URLs and compliance review.
- Click-through disclosure improvements.
- Recurring subscriptions, refunds, cancellation self-service, and payment
  reconciliation reporting.
- Plan-aware upgrade prompts for AI explanations, alerts, exports, comparison,
  watchlist size, and portfolio limits.
- Payment-provider reconciliation dashboard.
