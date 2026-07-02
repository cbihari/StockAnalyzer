# Deployment And Operations

## Purpose

Run the full product locally and in production with clear health checks, secrets boundaries, and provider configuration.

## Business Flow

1. Developer runs local Compose stack for integrated testing.
2. CI validates frontend, backend, ML, and Docker builds.
3. Production deploys frontend to Vercel, API/ML to Render, database to Neon.
4. Operators verify health endpoints and service logs.

## UI Flow

- Frontend is served by Angular dev server locally or static Vercel build in production.
- Production API calls use Vercel `/api` proxy to Render.

## Backend Flow

- API starts, configures CORS/auth, runs migrations, and maps controllers.
- ML service starts FastAPI routers and health endpoint.

## API Endpoints

- Frontend container health: `/health`
- .NET: `/health`, `/api/health`
- FastAPI: `/health`

## Models

Operational config is environment-variable based; see [13_Deployment.md](../13_Deployment.md).

## Validation

- Required production secrets must be present.
- Compose config should pass `docker compose config --quiet`.
- Render services must bind to supplied `PORT`.

## Database Tables

All EF-managed tables in [06_Database_Design.md](../06_Database_Design.md).

## Error Handling

- Startup should fail fast for missing production `JWT_SECRET`, `DATABASE_URL`, or `ML_SERVICE_URL`.
- OpenAI absence should not fail startup; it should use deterministic fallback.

## Future Improvements

- Add uptime monitoring.
- Add database backup/restore runbook.
- Add log aggregation and alerting guidance.
