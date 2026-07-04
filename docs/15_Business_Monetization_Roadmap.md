# Business Monetization Roadmap

StockAnalyzer should be operated as an educational stock research and learning
platform, not as a personalized investment-advice or stock-tip product. The
business model must monetize clarity, workflow, education, alerts, and research
productivity while preserving the product's existing disclaimer and safety
boundaries.

## Positioning

Primary positioning:

> AI-powered stock research and learning workspace for Indian retail investors.

The product promise is to help users understand a stock's trend, risk, evidence,
model estimate, news context, and historical reliability in plain language. The
product must not promise returns or tell users what to buy, sell, or hold.

## Compliance Boundary

This roadmap is a product and engineering plan, not legal advice. Before launch,
review the flow with a qualified securities-law professional, especially for
India-facing monetization.

The product must avoid:

- Personalized investment advice.
- Buy, sell, or hold recommendations.
- Target prices or guaranteed return language.
- Claims that model confidence equals profit probability.
- Arbitrary user prompts that can turn the assistant into a recommendation bot.
- Broker links without clear affiliate or sponsored-link disclosure.

The product may safely emphasize:

- Educational research.
- Technical and news signals.
- Supporting and conflicting evidence.
- Model limitations and historical accuracy.
- Risk factors, volatility, data freshness, and source labels.
- Learning content and portfolio tracking for user-entered holdings.

## Revenue Streams

| Stream | Near-term value | Notes |
| --- | --- | --- |
| Freemium SaaS | Recurring revenue from retail users | Best first monetization path. Gate advanced workflow, not basic education. |
| Broker affiliate links | Early non-subscription revenue | Requires disclosure, partner review, and click tracking. |
| Learning products | High-trust revenue | Courses, explainers, and workshops fit the educational positioning. |
| Creator/RA workspace | Higher-ticket B2B revenue | Let creators or registered professionals create compliant research pages. |
| API or white label | Later-stage platform revenue | Offer summaries, indicators, and explanation APIs once contracts are stable. |

## Packaging

Start with three plans.

| Plan | Target user | Included capabilities |
| --- | --- | --- |
| Free | Learners and first-time users | Limited stock analysis, market overview, basic watchlist, limited AI explanations. |
| Pro | Active retail researchers | More AI explanations, larger watchlist, alerts, comparison, portfolio insights, exports. |
| Power | Advanced users and creators | Higher usage limits, advanced model metrics, presentation/export mode, priority refresh. |

Initial India-friendly pricing targets:

- Free: INR 0.
- Pro: INR 299-499 per month.
- Power: INR 999 per month.

Pricing must be validated with real users before hard-coding long-term plan
limits.

## MVP Monetization Scope

The first paid milestone should add only the minimum surface needed to test
willingness to pay:

1. Account-required subscription state.
2. Plan limits for AI explanations, watchlist size, alerts, portfolio holdings,
   and exports.
3. Pricing page with Free, Pro, and Power.
4. Checkout integration through Razorpay or Stripe.
5. Upgrade prompts at natural usage limits.
6. Affiliate disclosure and broker-link tracking.
7. Analytics events for upgrade intent and paid-feature usage.

Avoid building a large admin billing system before validating payment intent.

## Feature Gating Matrix

| Capability | Free | Pro | Power |
| --- | --- | --- | --- |
| Stock analysis | Limited daily usage | Higher daily usage | Highest daily usage |
| AI explanations | Small daily quota | Larger quota and regenerate | Larger quota plus export-ready summaries |
| Watchlist | Small list | Larger list with notes | Larger list with digest and sorting |
| Alerts | Basic local alerts | Server-backed price and daily-move alerts | Signal-change and digest alerts |
| Portfolio | Limited holdings | Full manual portfolio summary | Export and deeper allocation/risk views |
| Comparison | Limited | Full 2-3 ticker comparison | Saved comparisons and share/export |
| Prediction history | Recent only | Filters and CSV export | Advanced model/outcome analytics |
| Learning center | Free core lessons | Progress and quizzes | Creator-ready learning/report exports |

## Product Roadmap

### Phase 1: Business Validation

Target: 1-2 weeks.

- Add pricing and business positioning to the product.
- Add analytics for search, analysis success, AI explanation clicks, watchlist
  adds, alert setup, portfolio usage, and upgrade CTA clicks.
- Interview 20 target users: beginners, active retail users, finance students,
  and finance creators.
- Confirm the first paid bundle and price sensitivity.
- Review all product copy for advice-like phrasing.

Exit criteria:

- Clear top three paid features.
- Clear first target segment.
- At least five users say they would pay for the proposed bundle.

### Phase 2: Paid MVP

Target: 3-5 weeks.

- Add subscription plan model and entitlement checks.
- Add Razorpay or Stripe checkout.
- Add plan-aware usage limits.
- Add upgrade page and in-context upgrade prompts.
- Improve affiliate disclosure and partner configuration.
- Send paid-feature and checkout events to analytics.
- Keep all prediction, AI, and research language educational.

Exit criteria:

- A user can sign up, pay, receive plan access, hit limits, and upgrade.
- Free users still get enough value to trust the product.
- No paid feature presents personalized investment advice.

### Phase 3: Retention

Target: 4-6 weeks.

- Add daily or weekly watchlist digest.
- Add email or in-app notification preferences.
- Add saved recent research and comparison history.
- Add portfolio health summary.
- Add SEO pages for high-volume Indian and US tickers.
- Add reusable educational pages for indicators and stock-analysis concepts.

Exit criteria:

- Users have a reason to return without searching manually.
- Organic landing pages can acquire users without paid ads.
- Retention can be measured by weekly active usage and watchlist/alert engagement.

### Phase 4: B2B Pilot

Target: 4-8 weeks after paid MVP.

- Build a creator/research workspace prototype.
- Add read-only share links with timestamps, disclaimers, and data sources.
- Add exportable research cards and presentation mode.
- Pilot with finance educators, creators, or registered professionals.
- Price pilots at INR 2,000-10,000 per month depending on workflow value.

Exit criteria:

- At least two B2B pilot users actively use the workflow.
- Export/share features reduce their manual research-publishing time.
- Legal/compliance review supports the target customer segment.

## Engineering Priorities

Build in this order:

1. Plan and entitlement domain model. Status: backend foundation added.
2. Usage-metering service. Status: backend foundation added.
3. Payment provider abstraction. Status: manual and Razorpay providers added.
4. Checkout and webhook handling. Status: manual checkout, Razorpay Payment
   Links/webhooks, and Razorpay Test Mode order verification added.
5. Pricing/upgrade frontend. Status: upgrade page connected to backend status
   and Razorpay Test Mode Checkout.
6. Limit-aware UI states. Status: upgrade page displays current usage and plan limits.
7. Analytics events.
8. Affiliate disclosure improvements.
9. Digest and retention jobs.
10. SEO and shareable research pages.

The first implementation should support local development without requiring real
payment credentials. Use deterministic test fixtures or provider sandbox mode
for local verification.

## Implemented Backend Foundation

The first development slice adds backend plan metadata and usage metering without
payment-provider dependency:

- `GET /api/monetization/status` returns Free/Pro/Power plan metadata and
  today's usage for the current workspace or signed-in user.
- `GET /api/monetization/usage/check` checks quota for a feature key and returns
  the recommended upgrade plan when a limit is reached.
- `POST /api/monetization/usage` records usage only when quota remains.
- `UsageEvents` stores daily feature usage by `X-Client-ID` or authenticated
  `UserId`.
- Anonymous users and users without an active subscription resolve to Free.
- `STOCKANALYZER_PLAN_OVERRIDE` can force a plan in local or staging
  environments.

Second development slice:

- `UserSubscriptions` stores provider-backed subscription state.
- Plan access now comes from `active` subscription rows, not from authentication
  status.
- `POST /api/monetization/checkout` creates a pending checkout session for a
  signed-in user.
- `IPaymentProvider` isolates checkout creation from the monetization service.
- `ManualPaymentProvider` is the local default and returns a redirect URL with a
  generated manual session ID.

Third development slice:

- `POST /api/monetization/webhooks/{provider}` accepts payment-provider events.
- `ManualPaymentProvider` parses local JSON webhook events for development.
- The monetization service updates `UserSubscriptions` to `active`, `past_due`,
  or `canceled` only after provider parsing succeeds.
- Real providers must verify signatures inside their `IPaymentProvider`
  implementation before returning normalized webhook events.

Fourth development slice:

- The Angular upgrade page calls `/api/monetization/status`.
- The page displays current plan, subscription state, and today's quota usage.
- Free/Pro/Power pricing cards are rendered from backend plan metadata.
- Signed-in users start Razorpay Test Mode Checkout through
  `/api/payments/create-order`.
- Anonymous users are routed to login before checkout.
- Angular receives only the Razorpay Test Mode Key ID and verifies completed
  payments through `/api/payments/verify`.

Fifth development slice:

- `POST /api/predictions/explain-ai/{ticker}` now checks the `ai_explanation`
  quota before creating a new generated explanation.
- Valid cached explanations are returned without consuming quota.
- Newly generated explanations record one usage event.
- Exhausted quota returns `402 Plan limit reached` before FastAPI/OpenAI is
  called.

Sixth development slice:

- The stock detail AI explanation panel now treats `402 Plan limit reached` as a
  quota state.
- Users see an upgrade prompt linking to `/upgrade` instead of a generic retry
  error when AI explanation quota is exhausted.
- Non-quota AI failures still show the retry path.

Seventh development slice:

- CSV export moved from browser-only generation to
  `GET /api/predictions/history/export`.
- The backend checks and records the `csv_export` quota before returning CSV.
- The prediction history page calls the backend export endpoint and shows an
  upgrade prompt when quota is exhausted.

Eighth development slice:

- Watchlist saves now check the `watchlist_item` stored limit before persistence.
- Alert state saves now check the `alert_rule` stored limit before persistence.
- The watchlist page rolls back optimistic local additions when the API returns
  `402 Plan limit reached` and links users to `/upgrade`.

Ninth development slice:

- Portfolio saves now check the `portfolio_holding` stored limit before
  persistence.
- The portfolio page rolls back optimistic local additions when the API returns
  `402 Plan limit reached` and links users to `/upgrade`.
- Frontend holding count display is no longer hardcoded to a single plan limit.

Tenth development slice:

- Added `MonetizationEvents` for bounded first-party funnel analytics.
- Added `POST /api/monetization/events` for approved quota, checkout, and
  paid-feature attempt events.
- Quota callout views/clicks are tracked for AI explanations, CSV exports,
  watchlist size, alert rules, and portfolio holdings.
- Checkout start/create/fail events are tracked from the upgrade page.

Eleventh development slice:

- Added admin-only `GET /api/monetization/events/funnel`.
- Added `/admin/monetization-funnel` to review aggregate event counts and
  source/feature/plan breakdowns.
- Funnel reporting is gated by `MONETIZATION_ADMIN_ENABLED` and the existing
  admin claim/role policy.

Twelfth development slice:

- Added admin-only `GET /api/monetization/events/funnel/export`.
- Added CSV export from `/admin/monetization-funnel` using the same enabled flag
  and admin policy as the report page.
- Export includes event totals and source/feature/plan breakdown rows.

Thirteenth development slice:

- Monetization funnel reports now include daily aggregate trend rows.
- `/admin/monetization-funnel` renders a compact daily trend bar view.
- Funnel CSV export includes daily rows in addition to event totals and
  source/feature/plan breakdowns.

Fourteenth development slice:

- Added `RazorpayPaymentProvider` behind `IPaymentProvider`.
- `PAYMENT_PROVIDER=razorpay` creates Razorpay Payment Links for Pro and Power.
- Razorpay webhooks verify `X-Razorpay-Signature` before activating access.
- A paid Razorpay payment link grants the selected plan for one billing period
  in the current MVP.

Fifteenth development slice:

- Added Razorpay Test Mode order checkout for the Angular upgrade page.
- `POST /api/payments/create-order` creates a Razorpay order and stores a
  pending subscription without exposing the Key Secret.
- `POST /api/payments/verify` validates the Razorpay signature against the
  server-stored order, verifies captured payment status and amount/currency via
  Razorpay, and activates the plan for one billing period.
- Live Razorpay keys are rejected by this integration; only `rzp_test_` keys are
  accepted.

Next development slice: add payment-provider reconciliation reporting, or build
retention features such as digest jobs and saved research history.

## Product Metrics

Track these before and after monetization:

- Visitor-to-analysis conversion.
- Search abandonment rate.
- Analysis success/error/fallback rate.
- AI explanation click-through rate.
- Watchlist add rate.
- Alert setup rate.
- Portfolio setup rate.
- Upgrade CTA click-through rate.
- Checkout started/completed.
- Free-to-paid conversion.
- Weekly active users.
- Paid churn.
- Affiliate click-through rate.

## Copy Rules

Use:

- "Educational research."
- "Model estimate."
- "Signals."
- "Risk factors."
- "Supporting evidence."
- "Conflicting evidence."
- "Data limitations."

Avoid:

- "Buy now."
- "Sell now."
- "Guaranteed profit."
- "Best stock to invest."
- "Personalized recommendation."
- "Target price."
- "Sure-shot call."

## Launch Checklist

- Legal/compliance review completed.
- All paid plan limits documented.
- Payment provider sandbox and production setup documented.
- Refund, cancellation, and support paths documented.
- Affiliate disclosure visible near partner links.
- Data freshness and provider labels visible in paid and free surfaces.
- AI fallback and model limitation states tested.
- No OpenAI key is exposed outside FastAPI.
- No paid feature bypasses the educational-research boundary.
