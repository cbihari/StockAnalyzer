# Deployment

See root [DEPLOYMENT.md](../DEPLOYMENT.md) for operator steps. This document captures architecture memory for future maintainers.

## Production Targets

| Component | Target | Config |
| --- | --- | --- |
| Angular | Vercel | `frontend/vercel.json` |
| .NET API | Render web service | `render.yaml`, `backend/Dockerfile` |
| FastAPI ML | Render web service with disk | `render.yaml`, `ml-service/Dockerfile` |
| PostgreSQL | Neon | `DATABASE_URL` |

## Important Production Rules

- Render supplies `PORT`; services must bind to it.
- ML model files need persistent disk at `/var/data/models` in Render.
- .NET requires `ML_SERVICE_URL` in production.
- `DATABASE_URL` should be a Neon pooled URL; the API converts it to an Npgsql connection with SSL verification.
- `ALLOWED_ORIGINS` must include the Vercel production URL.
- Vercel production frontend uses same-origin `/api` requests and proxies to Render.
- `OPENAI_API_KEY` is optional and belongs only to the ML service.

## Local vs Production Config

| Concern | Local | Production |
| --- | --- | --- |
| Frontend API URL | `http://localhost:8080` | same-origin `/api` proxy |
| Database | Compose PostgreSQL | Neon PostgreSQL |
| ML URL | `http://python-ml-service:8000` in Compose or localhost | Render service host/port |
| Models | `ml-service/models/` volume | Render disk |
| OpenAI key | `.secrets/openai_api_key` or env var | Render ML secret |

## Health Checks

- API: `/health` and `/api/health`
- ML: `/health`
- Frontend container: `/health`
