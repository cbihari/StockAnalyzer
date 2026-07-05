# Production Readiness

This document is the release checklist for StockAnalyzer. It reflects the current code and deployment files; verify live provider URLs and secrets before each release.

## Current Status

StockAnalyzer is structurally production-capable but not fully release-ready until the placeholder business items below are resolved. The current production target is:

- Angular frontend on Vercel.
- .NET API on Render.
- FastAPI ML service on Render with persistent model disk.
- PostgreSQL on Neon.

Known current strengths:

- Dockerfiles and Render Blueprint exist.
- Vercel SPA rewrite and `/api/*` proxy are configured.
- API startup applies EF Core migrations.
- Production API requires `JWT_SECRET`, `DATABASE_URL`, `ML_SERVICE_URL`, and `ALLOWED_ORIGINS`.
- OpenAI is isolated to the ML service and has deterministic fallback.
- Affiliate stats endpoint requires the affiliate admin authorization policy when enabled.

## Required Environment Variables

### .NET API

| Variable | Required | Notes |
| --- | --- | --- |
| `ASPNETCORE_ENVIRONMENT` | Yes | Use `Production`. |
| `DATABASE_URL` | Yes | Neon PostgreSQL pooled URL. Converted to Npgsql with SSL verification. |
| `ML_SERVICE_URL` | Yes | Render internal ML service host/port or full URL. |
| `ALLOWED_ORIGINS` | Yes | Comma-separated production frontend origins. |
| `FRONTEND_URL` | Yes for OAuth | Used for Google callback redirect. |
| `JWT_SECRET` | Yes | Generated/secret production signing key. |
| `GOOGLE_CLIENT_ID` | Optional | Enables Google sign-in only with non-placeholder secret pair. |
| `GOOGLE_CLIENT_SECRET` | Optional | Enables Google sign-in only with non-placeholder secret pair. |
| `AFFILIATE_ADMIN_ENABLED` | Optional | Keep false unless admin stats access is intentionally configured. |
| `AFFILIATE_LINKS_CONFIG` | Optional | JSON partner list overriding appsettings affiliate links. |
| `MONETIZATION_ADMIN_ENABLED` | Optional | Enables admin-only monetization funnel reporting. |
| `PAYMENT_PROVIDER` | Optional | Use `manual` for legacy local/testing checkout, `razorpay` only for the Razorpay Payment Links adapter. |
| `RAZORPAY_MODE` / `Razorpay:Mode` | Yes when Razorpay checkout is enabled | Use `test` by default; set `live` only on production with `rzp_live_*` credentials. |
| `RAZORPAY_KEY_ID` / `Razorpay:KeyId` | Yes when Razorpay checkout is enabled | Razorpay API key ID for the configured mode; safe for Checkout responses. |
| `RAZORPAY_KEY_SECRET` / `Razorpay:KeySecret` | Yes when Razorpay checkout is enabled | Backend-only Razorpay API key secret for orders and signature verification. |
| `RAZORPAY_WEBHOOK_SECRET` | Yes when Razorpay enabled | Razorpay webhook signing secret; keep separate from API credentials. |
| `RAZORPAY_PRO_AMOUNT_PAISE` | Optional | Defaults to `49900`. |
| `RAZORPAY_POWER_AMOUNT_PAISE` | Optional | Defaults to `99900`. |

### FastAPI ML Service

| Variable | Required | Notes |
| --- | --- | --- |
| `PYTHON_ENV` | Yes | Use `production`. |
| `MODEL_DIR` | Yes | Persistent model directory; Render uses `/var/data/models`. |
| `MARKET_DATA_PROVIDER` | Yes | Current value is `yahoo`. |
| `PORT` | Render | Render supplies this; container command must bind to it. |
| `OPENAI_API_KEY` | Optional | Enables grounded OpenAI responses. Keep only in ML service. |
| `AI_EXPLANATIONS_ENABLED` | Optional | Defaults true in local Compose. |
| `LLM_PROVIDER` | Optional | Defaults `openai`; deterministic fallback handles missing key. |
| `OPENAI_MODEL` | Optional | Current default in code is `gpt-5.5`. |
| `OPENAI_REASONING_EFFORT` | Optional | Current default `low`. |
| `OPENAI_TIMEOUT_SECONDS` | Optional | Current default `30`. |
| `AI_RATE_LIMIT_PER_MINUTE` | Optional | Current default `10`. |
| `TRAINING_WORKERS` | Optional | Controls background model training workers. |

### Frontend

Production Angular config uses same-origin `apiUrl: ''`; Vercel rewrites `/api/*` to the Render API. If the Render API domain changes, update `frontend/vercel.json`.

## Deployment Checklist

- Confirm `main` is the intended deploy branch for Vercel and Render.
- Confirm `frontend/vercel.json` points `/api/:path*` to the current Render API URL.
- Confirm Render Blueprint creates both `stockanalyzer-ml` and `stockanalyzer-api`.
- Confirm Render ML service has persistent disk mounted at `MODEL_DIR`.
- Confirm API `ALLOWED_ORIGINS` includes every production Vercel origin.
- Confirm `FRONTEND_URL` matches the final public frontend URL.
- Confirm Neon database exists and pooled `DATABASE_URL` is stored as a secret.
- Confirm Google OAuth redirect URI matches the production callback if Google login is enabled.
- If paid checkout is enabled, configure Razorpay keys for the intended mode:
  `RAZORPAY_MODE=test` with `rzp_test_*` keys for staging, or
  `RAZORPAY_MODE=live` with `rzp_live_*` keys for the production URL. Set
  `PAYMENT_PROVIDER=razorpay` only if the legacy Payment Links adapter is also
  used, and point Razorpay webhooks to `/api/monetization/webhooks/razorpay`.
- Deploy ML service before or alongside API so `ML_SERVICE_URL` resolves.
- Verify health endpoints after deploy:

```bash
curl https://stockanalyzer-api.onrender.com/health
curl https://stockanalyzer-api.onrender.com/api/health
curl https://stockanalyzer-ml.onrender.com/health
```

## Security Checklist

- Do not expose `OPENAI_API_KEY` outside the ML service.
- Do not expose `JWT_SECRET`, `DATABASE_URL`, Google secrets, or real affiliate config in source control.
- Keep `ALLOWED_ORIGINS` restricted to HTTPS production frontend origins.
- Keep `AFFILIATE_ADMIN_ENABLED=false` unless an admin user/claim process is configured.
- If affiliate stats are enabled, grant access only to users with `Admin` role or `stockanalyzer_admin=true` claim.
- If Razorpay is enabled, verify webhook signature checks with the provider
  dashboard before accepting production payments.
- Validate Google OAuth client ID/secret are real and callback origins are correct before enabling Google login.
- Confirm public endpoints remain intentionally public: stock research, auth config/login/signup, affiliate partners, affiliate click tracking.
- Confirm educational/not-financial-advice copy remains visible in prediction and AI flows.

## Database Migration Checklist

- Review new EF Core migrations before release.
- Confirm migration snapshot matches domain/configuration changes.
- Confirm API startup can connect to Neon and run migrations.
- Confirm `DATABASE_URL` is a pooled Neon URL and SSL is required.
- Take a Neon backup/snapshot before deploying schema changes that affect existing data.
- Smoke test persisted flows after migration:
  - signup/login
  - watchlist save/load
  - portfolio save/load and summary
  - prediction history
  - AI explanation cache metadata

## ML Service Checklist

- Confirm `/health` returns healthy and expected market provider.
- Confirm `MODEL_DIR` persists across restarts.
- Confirm model training can write model, metrics, versions, and job files.
- Confirm `/stock/history`, `/stock/indicators`, `/predict/ml`, `/models/train`, and `/models/versions` work for a known ticker.
- Confirm deterministic fallback works when `OPENAI_API_KEY` is absent.
- If `OPENAI_API_KEY` is set, confirm `/ai/explain` and `/ai/research` return structured grounded responses and do not leak secrets.
- Monitor provider limitations; Yahoo-backed data is not a licensed production market-data feed.

## Monitoring And Logging Recommendations

- Track uptime for frontend, API `/health`, and ML `/health`.
- Alert on API 5xx rate, ML 5xx rate, and gateway 502/504 responses.
- Log and monitor `X-Correlation-ID` across .NET and FastAPI.
- Track prediction fallback rate and model training failures.
- Track AI fallback reason counts: missing key, timeout, rate limit, refusal, provider unavailable.
- Track background training job failures and interrupted jobs after service restarts.
- Track database connection errors and migration startup failures.
- Track affiliate click volume only after compliance review and admin access setup.

## Known Placeholder Items

- Affiliate partner URLs in `backend/appsettings.json` currently use `https://example.com/...`; replace with approved production partner URLs or supply `AFFILIATE_LINKS_CONFIG`.
- Razorpay order checkout is wired for the upgrade page with explicit
  test/live mode validation, and Razorpay Payment Links remain behind
  `IPaymentProvider`. Recurring billing, refunds, cancellation self-service,
  and reconciliation reporting are still product gaps before a full paid launch.

## Pre-Release Verification Commands

Run from the repository root unless noted:

```bash
dotnet test tests/StockAnalyzer.UnitTests/StockAnalyzer.UnitTests.csproj
dotnet test backend/StockAnalyzer.Tests/StockAnalyzer.Tests.csproj
dotnet build backend/StockAnalyzer.Api.csproj --configuration Release

cd frontend
npm test -- --watch=false --browsers=ChromeHeadless
npm run build
cd ..

cd ml-service
.venv/bin/python -m pytest -q
cd ..

docker compose config --quiet
docker compose build
git diff --check
```

Optional integrated smoke test:

```bash
cp .env.example .env
docker compose up --build
curl http://localhost:8080/api/health
curl http://localhost:8000/health
curl "http://localhost:8080/api/stocks/search?query=rel"
curl "http://localhost:8080/api/stocks/AAPL/analysis?period=1y"
```
