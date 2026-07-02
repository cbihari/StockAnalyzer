# Testing

## Test Locations

| Area | Path | Typical command |
| --- | --- | --- |
| Angular | `frontend/src/**/*.spec.ts` | `cd frontend && npm test -- --watch=false --browsers=ChromeHeadless` |
| .NET unit | `tests/StockAnalyzer.UnitTests/` | `dotnet test tests/StockAnalyzer.UnitTests/StockAnalyzer.UnitTests.csproj` |
| .NET backend | `backend/StockAnalyzer.Tests/` | `dotnet test backend/StockAnalyzer.Tests/StockAnalyzer.Tests.csproj` |
| FastAPI/ML | `ml-service/tests/` | `cd ml-service && .venv/bin/pytest -q` |
| Compose validation | root | `docker compose config --quiet && docker compose build` |

## CI

`.github/workflows/ci.yml` runs:

- Node 22 frontend install, tests, build.
- .NET 9 unit tests and release build.
- Python 3.13 dependency install and pytest.
- Docker Compose config validation and build.

## Coverage Expectations

- API contract changes require controller/service tests.
- ML calculation or training changes require Python unit tests with deterministic fixtures.
- Frontend flow changes require component/service tests for validation, loading, error, and success states.
- Database changes require migration-aware tests where practical.
- AI behavior changes require fallback tests and schema validation tests.

## Manual Smoke Checks

- `GET /api/health`
- `GET /health` on FastAPI
- Search for `RELIANCE.NS` and `AAPL`
- Open stock detail and verify analysis, chart, prediction, and fallback metadata
- Save watchlist/portfolio and reload
- Run AI explanation with and without `OPENAI_API_KEY`
