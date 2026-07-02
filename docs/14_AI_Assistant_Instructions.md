# AI Assistant Instructions

This repository documentation is intended as project memory for Codex, Claude Code, ChatGPT, Cursor, and similar assistants.

## Non-Negotiable Boundaries

- Do not change production code when the task is documentation-only.
- OpenAI must only be called from the Python FastAPI service.
- Never expose `OPENAI_API_KEY` to Angular, .NET responses, logs, docs examples with real values, or browser storage.
- Do not allow arbitrary user prompts in first-version AI flows; keep AI grounded in StockAnalyzer evidence.
- Preserve not-financial-advice disclaimers and educational framing.
- Prefer deterministic fallback behavior over broken AI/user flows.
- Do not delete or overwrite generated/user data unless explicitly asked.

## How To Orient Quickly

1. Read [00_Project_Overview.md](00_Project_Overview.md).
2. Read [02_System_Architecture.md](02_System_Architecture.md).
3. Read the relevant feature doc in [Features/](Features/).
4. Check [05_API_Documentation.md](05_API_Documentation.md) and [06_Database_Design.md](06_Database_Design.md) if APIs or persistence are involved.
5. Inspect the actual source before editing; docs are memory, not a substitute for verification.

## Preferred Implementation Patterns

- Angular: standalone components, signals, typed services, local component state.
- .NET: controller -> application service -> abstraction -> infrastructure repository/client.
- Python: FastAPI router -> service/helper -> Pydantic schemas -> deterministic tests.
- Persistence: domain entity + EF configuration + migration + repository + tests.
- AI: FastAPI schema/provider/context builder + deterministic fallback + .NET gateway DTO + Angular opt-in UI.

## Verification Expectations

- Frontend change: run focused Angular tests and `npm run build` when feasible.
- Backend change: run relevant `dotnet test` and `dotnet build`.
- ML change: run `.venv/bin/pytest -q` from `ml-service`.
- Cross-service change: use Docker Compose health/API smoke checks when feasible.

## Documentation Updates

When behavior changes, update the numbered docs and the relevant feature doc. Keep docs concise, cross-linked, and non-duplicative.
