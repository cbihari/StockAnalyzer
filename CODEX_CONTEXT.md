# Codex Context

Compact project memory for future AI coding sessions. Treat current source code as the authority; older docs may lag.

## 1. Project Overview

StockAnalyzer is an educational stock-analysis dashboard. Users search stocks, review historical prices, indicators, news, market context, watchlists, portfolios, prediction history, model accuracy, and optional grounded AI explanations. The product must never present output as financial advice or a buy/sell recommendation.

## 2. Tech Stack

- Frontend: Angular 20 standalone components, signals, Chart.js, SCSS.
- API: ASP.NET Core 9 Web API, EF Core, ASP.NET Identity, JWT, Google OAuth optional.
- ML service: FastAPI, yfinance provider boundary, pandas/scikit-learn Random Forest models, OpenAI SDK only inside FastAPI.
- Database: PostgreSQL via Npgsql/EF Core migrations.
- Local orchestration: Docker Compose.
- Production: Vercel frontend, Render API/ML services, Neon PostgreSQL.

## 3. Architecture Flow

```text
Angular browser app
  -> .NET API gateway
  -> FastAPI ML service
  -> market data/model files/OpenAI when enabled

.NET API
  -> PostgreSQL for stocks, prices, predictions, workspaces, users, affiliate clicks, AI metadata
```

Critical boundary: Angular never calls FastAPI or OpenAI directly. .NET never calls OpenAI. OpenAI keys and calls stay in `ml-service/app/ai/`.

## 4. Important Folders

- `frontend/src/app/pages/`: routed Angular pages.
- `frontend/src/app/core/`: `StockApiService`, frontend models, watchlist/portfolio/alert/client identity state.
- `frontend/src/app/auth/`: auth service, token store, interceptor, login/signup/callback/profile UI.
- `backend/Controllers/`: public .NET REST API.
- `backend/StockAnalyzer.Application/`: DTOs, abstractions, orchestration services.
- `backend/StockAnalyzer.Domain/`: entities/value objects.
- `backend/StockAnalyzer.Infrastructure/`: EF Core, repositories, typed FastAPI client.
- `ml-service/app/`: FastAPI routes, market data, indicators, model training/registry, AI.
- `ml-service/tests/`, `tests/StockAnalyzer.UnitTests/`, `backend/StockAnalyzer.Tests/`, `frontend/src/**/*.spec.ts`: tests.

## 5. Key Backend Controllers And Services

Controllers:
- `StocksController`: quotes, market overview, comparison, analysis, news, search, history, indicators.
- `PredictionsController`: rule-based/ML prediction, evaluation, history, deterministic and AI explanations.
- `ModelController`: accuracy, metrics, synchronous/background training, job status, model versions.
- `WorkspaceController`: guest/auth watchlist, alerts, portfolio JSON state.
- `PortfolioController`: delayed manual portfolio summary.
- `ResearchController`: ticker-scoped AI research answers.
- `AuthController`, `AccountController`: signup/login/Google/me and workspace claiming.
- `AffiliateController`: partner links, click tracking, config-gated stats.

Services:
- `StockAnalysisService`: main .NET orchestration for ML/history/indicators/predictions.
- `PredictionEvaluationService`: evaluates pending predictions against next available close.
- `GuestWorkspaceService`: validates and serializes workspace JSON.
- `PortfolioService`: calculates holdings summary from workspace holdings and quote batches.
- `AiExplanationService`, `AiResearchService`: .NET gateway services to FastAPI AI endpoints.
- `MlServiceClient`: typed FastAPI client; update this when ML route contracts change.

## 6. Key Frontend Services And Pages

Routes are in `frontend/src/app/app.routes.ts`.

Important pages:
- `/`, `/search`, `/stocks/:ticker`, `/stocks/:ticker/news`
- `/market`, `/compare`, `/assistant`
- `/watchlist`, `/portfolio`, `/notifications`
- `/history`, `/accuracy`, `/learn`, `/upgrade`
- `/login`, `/signup`, `/auth/callback`, `/admin/affiliate-stats`

Important services:
- `StockApiService`: all .NET API calls.
- `WatchlistService`, `AlertService`, `PortfolioService`: browser-local state plus workspace sync.
- `ClientIdentityService`: anonymous `X-Client-ID`.
- `AuthService`, `AuthTokenStore`, `authInterceptor`: JWT/session auth.
- `ticker-validation.ts`: frontend ticker normalization.

## 7. Key ML Service Routes

Implemented FastAPI routes:
- `GET /health`
- `POST /predict` legacy deterministic placeholder
- `GET /stock/history?ticker=&period=`
- `GET /stock/indicators?ticker=&period=`
- `GET /predict/rule-based?ticker=`
- `GET /predict/ml?ticker=`
- `GET /stocks/search?query=`
- `GET /stocks/quotes?tickers=`
- `GET /stocks/news?ticker=&lookback_days=&limit=`
- `GET /market/overview?region=`
- `GET /models/metrics?ticker=`
- `GET /models/versions?ticker=`
- `POST /models/train?ticker=&period=`
- `POST /models/train/jobs?ticker=&period=`
- `GET /models/train/jobs/{job_id}`
- `GET /models/train/jobs?ticker=`
- `POST /ai/explain`
- `POST /ai/research`

## 8. Database And EF Core

`StockAnalyzerDbContext` extends Identity DB context and exposes:
- `Stocks`, `StockPrices`, `Predictions`, `ModelMetrics`
- `GuestWorkspaces`, `AiExplanations`, `AffiliateClicks`
- ASP.NET Identity `AspNet*` tables

Configuration classes live in `backend/StockAnalyzer.Infrastructure/Persistence/Configurations/`. Migrations live in `backend/StockAnalyzer.Infrastructure/Persistence/Migrations/`. API startup applies migrations through `MigrateDatabaseAsync()`.

Important persistence rules:
- `GuestWorkspaces` stores watchlist, alert state, and portfolio as JSONB.
- `AiExplanations` stores structured AI output JSON plus provider/model/cache metadata.
- `Predictions` are tied to `UserId` or anonymous `WorkspaceId`.
- Neon `DATABASE_URL` is converted to an Npgsql connection string with SSL verification.

## 9. Deployment Overview

- Local: `docker compose up --build`.
- Frontend: Vercel, root `frontend`, same-origin `/api/*` proxy to Render API.
- API: Render Docker service `stockanalyzer-api`; requires `DATABASE_URL`, `ML_SERVICE_URL`, `ALLOWED_ORIGINS`, production `JWT_SECRET`.
- ML: Render Docker service `stockanalyzer-ml`; optional `OPENAI_API_KEY`; persistent `MODEL_DIR` disk at `/var/data/models`.
- Database: Neon PostgreSQL.
- Render supplies `PORT`; do not hardcode production ports.

## 10. Test Commands

```bash
dotnet test tests/StockAnalyzer.UnitTests/StockAnalyzer.UnitTests.csproj
dotnet test backend/StockAnalyzer.Tests/StockAnalyzer.Tests.csproj
dotnet build backend/StockAnalyzer.Api.csproj --configuration Release

cd frontend && npm test -- --watch=false --browsers=ChromeHeadless
cd frontend && npm run build

cd ml-service && .venv/bin/python -m pytest -q

docker compose config --quiet
git diff --check
```

## 11. Known Technical Debt From Audit

- Stock detail analysis causes repeated history/indicator/prediction work and currently records predictions during normal read flows.
- Workspace sync can race: watchlist/alerts/portfolio use local storage plus immediate API sync without versioning/debounce/conflict handling.
- Affiliate stats are config-gated but not role-protected if `AFFILIATE_ADMIN_ENABLED=true`.
- Upgrade/affiliate surfaces still use placeholder partner URLs. Razorpay Test
  Mode order checkout is wired for paid plans; live-key production checkout is
  intentionally not enabled.
- Documentation was corrected for FastAPI route prefixes, but re-check docs after route changes.

## 12. Safe Areas For Future Refactoring

- Consolidate repeated route/API documentation after source verification.
- Add FastAPI route contract tests for `MlServiceClient`.
- Add caching or a bundled analysis endpoint to reduce repeated provider calls.
- Improve frontend workspace sync with debounced saves, request ordering, and `updatedAt` conflict handling.
- Extract large Angular inline templates/styles into component files where it improves readability without changing behavior.
- Add authorization/role checks around admin-like features.

## 13. Avoid Changing Without Explicit Approval

- Do not move OpenAI calls or keys into Angular or .NET.
- Do not expose `OPENAI_API_KEY`, JWT secrets, database URLs, or real provider credentials in docs, logs, responses, or frontend code.
- Do not remove educational/not-financial-advice language.
- Do not change prediction semantics, training thresholds, model registry paths, or persistence behavior casually.
- Do not rewrite architecture across service boundaries without a clear migration plan.
- Do not delete generated models, processed data, migrations, or user/workspace data unless explicitly requested.
- Do not revert unrelated dirty worktree changes.
