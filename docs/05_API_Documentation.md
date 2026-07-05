# API Documentation

The browser calls only the .NET API. The .NET API calls FastAPI. FastAPI is not a direct browser contract except during local/debug API testing.

## .NET API

Base URL:

- Local: `http://localhost:8080`
- Production: Render API behind Vercel `/api/*` proxy where configured

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/health`, `/api/health` | API health. |
| GET | `/api/stocks/search?query=` | Company/ticker autocomplete. |
| GET | `/api/stocks/{ticker}/analysis?period=1y` | Unified stock research snapshot. |
| GET | `/api/stocks/compare?tickers=AAPL,MSFT&period=1y` | Compare 2-3 stock analyses. |
| GET | `/api/stocks/market/overview?region=india` | India/US index and breadth snapshot. |
| GET | `/api/stocks/quotes?tickers=AAPL,MSFT` | Batch quote snapshot. |
| GET | `/api/stocks/{ticker}/news?lookbackDays=7&limit=10` | Source-linked news with deterministic labels. |
| GET | `/api/stocks/history/{ticker}?period=5y` | Historical OHLCV data. |
| GET | `/api/stocks/indicators/{ticker}?period=5y` | Technical indicator rows. |
| GET | `/api/predictions/rule-based/{ticker}` | Deterministic rule-based prediction. |
| GET | `/api/predictions/ml/{ticker}` | ML prediction, with training or fallback as needed. |
| POST | `/api/predictions/evaluate` | Evaluate pending predictions against actual next close. |
| GET | `/api/predictions/history?outcome=all&limit=100` | Persisted prediction history. |
| GET | `/api/predictions/history/export?outcome=all&limit=500` | CSV export of prediction history with `csv_export` quota enforcement. |
| GET | `/api/predictions/explain/{ticker}` | Deterministic explanation. |
| POST | `/api/predictions/explain-ai/{ticker}?forceRefresh=false` | Grounded AI explanation with fallback/cache and `ai_explanation` quota enforcement. |
| POST | `/api/research/{ticker}` | Ticker-scoped research assistant answer. |
| GET | `/api/model/accuracy` | Overall evaluated prediction accuracy. |
| GET | `/api/model/metrics/{ticker}` | Ticker model metrics. |
| POST | `/api/model/train/{ticker}?period=5y` | Synchronous retrain. |
| POST | `/api/model/train/{ticker}/jobs?period=5y` | Queue background training job. |
| GET | `/api/model/train/jobs/{jobId}` | Training job status. |
| GET | `/api/model/versions/{ticker}` | Immutable model versions. |
| GET/PUT | `/api/workspace/watchlist` | Guest/auth workspace watchlist with `watchlist_item` stored-limit enforcement on save. |
| GET/PUT | `/api/workspace/alerts` | Guest/auth alert rules and notifications with `alert_rule` stored-limit enforcement on save. |
| GET/PUT | `/api/workspace/portfolio` | Guest/auth manual portfolio holdings with `portfolio_holding` stored-limit enforcement on save. |
| GET | `/api/portfolio/summary` | Delayed valuation and allocation summary. |
| GET | `/api/affiliate/partners` | Configured partner links. |
| POST | `/api/affiliate/click` | Track broker click. |
| GET | `/api/affiliate/stats` | Admin-enabled aggregate click stats. |
| GET | `/api/monetization/status` | Plan catalog and current workspace/user usage. |
| GET | `/api/monetization/usage/check?featureKey=ai_explanation&quantity=1` | Check whether a feature has remaining quota. |
| POST | `/api/monetization/usage` | Record usage for a monetized feature when quota is available. |
| POST | `/api/monetization/events` | Record bounded first-party monetization analytics events. |
| GET | `/api/monetization/events/funnel?days=30` | Admin aggregate report for monetization funnel events. |
| GET | `/api/monetization/events/funnel/export?days=30` | Admin CSV export for monetization funnel events. |
| POST | `/api/monetization/checkout` | Create an authenticated paid-plan checkout session. |
| POST | `/api/monetization/webhooks/{provider}` | Process a payment-provider webhook and update subscription state. |
| POST | `/api/payments/create-order` | Create an authenticated Razorpay order for Checkout using the configured mode. |
| POST | `/api/payments/verify` | Verify Razorpay payment/order/signature before activating access. |
| GET | `/api/auth/config` | Auth provider availability. |
| POST | `/api/auth/signup` | Email/password signup. |
| POST | `/api/auth/login` | Email/password login. |
| GET | `/api/auth/google` | Google OAuth challenge if configured. |
| GET | `/api/auth/google/callback` | Google OAuth callback. |
| GET | `/api/auth/me` | Current JWT user. |
| POST | `/api/account/claim-workspace` | Claim guest workspace into logged-in account. |

## Required Headers

| Header | Used by | Purpose |
| --- | --- | --- |
| `Authorization: Bearer <jwt>` | authenticated endpoints | Optional identity for workspace/user flows; required by `/api/auth/me` and `/api/account/claim-workspace`. |
| `X-Client-ID` | workspace, portfolio, affiliate, monetization usage, prediction history | Browser-generated anonymous workspace identifier. |
| `X-Correlation-ID` | all service calls | Optional request tracing ID; generated if absent. |

## FastAPI ML Service

Base URL:

- Local: `http://localhost:8000`
- Internal production: Render private service URL from `ML_SERVICE_URL`

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/health` | ML service health and market provider. |
| GET | `/stock/history?ticker=&period=` | Historical prices. |
| GET | `/stock/indicators?ticker=&period=` | Technical indicators. |
| GET | `/predict/rule-based?ticker=` | Rule-based prediction. |
| GET | `/predict/ml?ticker=` | ML prediction. |
| GET | `/stocks/search?query=` | Stock suggestions. |
| GET | `/stocks/quotes?tickers=` | Batch quotes. |
| GET | `/market/overview?region=` | Market overview. |
| GET | `/stocks/news?ticker=&lookback_days=&limit=` | News and labels. |
| GET | `/models/metrics?ticker=` | Model metrics. |
| POST | `/models/train?ticker=&period=` | Train/retrain model. |
| POST | `/models/train/jobs?ticker=&period=` | Queue training job. |
| GET | `/models/train/jobs/{job_id}` | Training job status. |
| GET | `/models/train/jobs?ticker=` | Recent training jobs. |
| GET | `/models/versions?ticker=` | Model versions. |
| POST | `/ai/explain` | AI explanation request. |
| POST | `/ai/research` | AI research request. |

## Error Shape

The .NET API returns `ProblemDetails` with a correlation ID on handled exceptions. FastAPI returns validation errors from Pydantic/FastAPI or generic unexpected errors with `correlation_id`.

## Monetization Usage Keys

The first monetization slice supports deterministic Free/Pro/Power metadata and
usage checks without requiring payment-provider credentials. Current feature
keys:

- `stock_analysis`
- `ai_explanation`
- `watchlist_item`
- `alert_rule`
- `portfolio_holding`
- `csv_export`

Anonymous users and authenticated users without an active subscription resolve
to the Free plan. Active `UserSubscriptions` rows determine paid plan access.
Local or staging environments can override plan resolution with
`STOCKANALYZER_PLAN_OVERRIDE`.

`POST /api/monetization/checkout` requires `Authorization: Bearer <jwt>` and
creates a pending subscription through the configured payment provider. The
default provider is `manual`, which returns a deterministic redirect URL for
local integration testing. Paid access begins only after a subscription is
stored with `status=active`.

`POST /api/payments/create-order` requires `Authorization: Bearer <jwt>` and
creates a Razorpay order for the selected paid plan using `RAZORPAY_MODE`
(`test` by default, `live` only when explicitly configured). The response
returns the Razorpay Key ID, order ID, amount, currency, `checkoutMode`, and
Checkout display fields for Angular. It never returns the Key Secret. After
Checkout completes, Angular posts `razorpay_payment_id`,
`razorpay_order_id`, and `razorpay_signature` to `POST /api/payments/verify`.
The backend loads the stored order reference, validates the signature with
`Razorpay:KeySecret` or `RAZORPAY_KEY_SECRET`, fetches the Razorpay payment,
and requires captured status plus the expected order, amount, and currency
before activating the pending subscription for one billing period. In test
mode, `Razorpay:KeyId` must start with `rzp_test_`; in live mode, it must start
with `rzp_live_`.

Set `PAYMENT_PROVIDER=razorpay` to use the Razorpay Payment Links adapter. The
adapter creates a `POST /v1/payment_links` request with the selected plan amount
in paise, stores the Razorpay payment-link ID as the provider checkout session
ID, and redirects the user to Razorpay's short payment URL. This MVP treats a
paid Razorpay payment link as one billing period of access, currently one month
from webhook processing time.

For local/manual webhook testing, post JSON to
`/api/monetization/webhooks/manual`:

```json
{
  "eventType": "subscription.active",
  "status": "active",
  "planKey": "pro",
  "providerCheckoutSessionId": "manual_checkout_id",
  "providerSubscriptionId": "manual_subscription_id",
  "providerCustomerId": "manual_customer_id",
  "currentPeriodEnd": "2026-08-02T00:00:00Z"
}
```

Razorpay webhooks should be sent to `/api/monetization/webhooks/razorpay`. The
provider verifies `X-Razorpay-Signature` with `RAZORPAY_WEBHOOK_SECRET` before
returning a normalized event to the monetization service. `payment_link.paid`,
paid payment-link status, or captured payment status activates the matching
pending subscription.

`POST /api/predictions/explain-ai/{ticker}` checks the `ai_explanation` quota
before generating a new explanation. Valid cached explanation responses do not
consume quota. If the current plan has no remaining quota, the API returns
`402 application/problem+json` with title `Plan limit reached`.

`GET /api/predictions/history/export` checks and records the `csv_export` quota
before returning `text/csv`. If quota is exhausted, it returns `402
application/problem+json` with title `Plan limit reached`.

`PUT /api/workspace/watchlist`, `PUT /api/workspace/alerts`, and
`PUT /api/workspace/portfolio` check stored plan limits before saving. Watchlist
saves use `watchlist_item`; alert-rule saves use `alert_rule`; portfolio saves
use `portfolio_holding`. If the submitted state exceeds the current plan, the
API returns `402 application/problem+json` and does not persist the oversized
workspace state.

`POST /api/monetization/events` records first-party monetization analytics for
quota callout views/clicks, paid-feature attempts, and checkout start/create/fail
events. Payloads are bounded to approved event names, feature keys, plan keys,
source names, and short metadata values. Do not send ticker searches, personal
notes, or arbitrary user prompts.

`GET /api/monetization/events/funnel` and
`GET /api/monetization/events/funnel/export` require the admin policy and
`MONETIZATION_ADMIN_ENABLED=true` or the existing affiliate admin enable flag.
They return aggregate event counts, source/feature/plan breakdowns, and daily
trend rows only; raw event payloads are not exposed.
