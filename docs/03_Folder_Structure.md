# Folder Structure

## Repository Root

| Path | Purpose |
| --- | --- |
| `README.md` | User-facing product, setup, and API overview. |
| `DEPLOYMENT.md` | Production deployment guide for Neon, Render, and Vercel. |
| `docker-compose.yml` | Local multi-service stack. |
| `render.yaml` | Render Blueprint for API and ML services. |
| `docs/` | Architecture and feature memory for humans and AI assistants. |
| `.github/workflows/ci.yml` | Frontend, backend, ML, and Docker CI. |

## Frontend

| Path | Purpose |
| --- | --- |
| `frontend/src/app/app.routes.ts` | Canonical Angular route map. |
| `frontend/src/app/pages/` | Page-level standalone components. |
| `frontend/src/app/core/` | API service, models, browser state services. |
| `frontend/src/app/auth/` | Login, signup, callback, token store, auth interceptor. |
| `frontend/src/app/shared/` | Reusable UI components such as charts, autocomplete, badges, tips. |
| `frontend/src/environments/` | Local and production API/auth URLs. |

## Backend

| Path | Purpose |
| --- | --- |
| `backend/Controllers/` | Public REST controllers. |
| `backend/Auth/` | JWT, auth provider config, claims helpers. |
| `backend/Middleware/` | Request logging and exception handling. |
| `backend/StockAnalyzer.Domain/` | Domain entities and value objects. |
| `backend/StockAnalyzer.Application/` | DTOs, service interfaces, business workflows. |
| `backend/StockAnalyzer.Infrastructure/` | EF Core, repositories, typed FastAPI client. |
| `backend/StockAnalyzer.Infrastructure/Persistence/Migrations/` | EF Core migrations. |

## ML Service

| Path | Purpose |
| --- | --- |
| `ml-service/app/main.py` | FastAPI app, router registration, health, request logging. |
| `ml-service/app/stock_history.py` | Historical OHLCV endpoint. |
| `ml-service/app/indicators.py` | Technical indicators. |
| `ml-service/app/ml_prediction.py` | ML prediction endpoint. |
| `ml-service/app/model_training.py` | Training pipeline. |
| `ml-service/app/model_registry.py` | Model metadata and immutable versions. |
| `ml-service/app/training_jobs.py` | Background training job state. |
| `ml-service/app/ai/` | Grounded AI explanation/research implementation. |
| `ml-service/data/stock_master.csv` | Search/autocomplete seed data. |
| `ml-service/models/` | Generated trained model files and registry. |

## Tests

| Path | Purpose |
| --- | --- |
| `frontend/src/**/*.spec.ts` | Angular unit/component tests. |
| `tests/StockAnalyzer.UnitTests/` | Main .NET unit tests. |
| `backend/StockAnalyzer.Tests/` | Additional backend tests. |
| `ml-service/tests/` | FastAPI/ML tests. |
