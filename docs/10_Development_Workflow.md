# Development Workflow

## Local Setup

```bash
cp .env.example .env
docker compose up --build
```

Local URLs:

- Frontend: `http://localhost:4200`
- .NET API: `http://localhost:8080`
- FastAPI: `http://localhost:8000`
- PostgreSQL: `localhost:5433`

## Direct Service Commands

Frontend:

```bash
cd frontend
npm ci
npm start
```

.NET:

```bash
dotnet build backend/StockAnalyzer.Api.csproj
dotnet test tests/StockAnalyzer.UnitTests/StockAnalyzer.UnitTests.csproj
```

ML service:

```bash
cd ml-service
python -m venv .venv
.venv/bin/pip install -r requirements.txt
.venv/bin/pytest -q
```

## API Inspection

- .NET Swagger: `http://localhost:8080/swagger`
- FastAPI Swagger: `http://localhost:8000/docs`

## Change Workflow

1. Identify the owning feature doc under [Features/](Features/).
2. Update the smallest relevant layer set.
3. Add/update tests at the layer where behavior changes.
4. Run focused tests first, then broader build/test checks.
5. Update docs if routes, database schema, state, deployment, AI behavior, or user flows changed.

## Common Gotchas

- First-time ML prediction can be slow because it may train a ticker model.
- OpenAI fallback is expected when no key is configured.
- Render supplies `PORT`; do not hardcode production ports.
- Neon `DATABASE_URL` must use SSL verification after conversion.
- Angular production uses same-origin `/api` via Vercel proxy.
