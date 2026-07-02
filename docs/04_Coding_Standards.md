# Coding Standards

## General

- Preserve the current layered boundaries.
- Keep public DTOs explicit and typed; do not leak EF entities or provider-specific market-data models through public APIs.
- Validate and normalize tickers at service boundaries.
- Keep educational disclaimers visible for predictions and AI outputs.
- Do not commit secrets, generated trained models, local databases, or `.env` values.

## Angular

- Prefer standalone components and Angular signals for local page state.
- Keep API calls in `frontend/src/app/core/stock-api.service.ts` or auth-specific services.
- Keep shared TypeScript interfaces in `frontend/src/app/core/models.ts`.
- Use route definitions from `frontend/src/app/app.routes.ts`; avoid hidden navigation paths.
- UI state should include loading, empty, validation, error, and success states for remote workflows.
- Persist guest-only state defensively with `try/catch` around `localStorage` or `sessionStorage`.

## .NET

- Controllers should remain thin and call application services.
- Application services own orchestration and business rules.
- Infrastructure owns EF Core repositories and FastAPI client calls.
- Domain classes stay persistence-friendly but should not depend on controllers or infrastructure.
- Use async APIs and pass `CancellationToken`.
- Return `ProblemDetails` for bad requests and failures.
- Add EF mappings in configuration classes, not inline in `DbContext`.

## Python/FastAPI

- Keep route modules focused by feature.
- Use Pydantic models for request/response validation.
- Normalize ticker input with existing helpers.
- Use deterministic fallback paths for unavailable market data, model limitations, and AI failures where the product expects a safe response.
- Maintain model registry metadata whenever training behavior changes.

## Documentation

- Update feature docs in [Features/](Features/) when changing behavior.
- Update [05_API_Documentation.md](05_API_Documentation.md) when endpoint contracts change.
- Update [06_Database_Design.md](06_Database_Design.md) when migrations/entities change.
- Update [14_AI_Assistant_Instructions.md](14_AI_Assistant_Instructions.md) when architectural guardrails change.
