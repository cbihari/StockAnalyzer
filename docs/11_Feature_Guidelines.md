# Feature Guidelines

Use this checklist when adding or modifying features.

## Feature Design

- State the user problem and education value.
- State whether the feature is free, paid, affiliate-supported, or internal.
- Confirm the feature sells workflow, education, alerts, export, or productivity
  value rather than personalized investment advice.
- Decide whether the feature is frontend-only, .NET-only, ML-only, or cross-service.
- Confirm whether it needs persistence.
- Confirm whether it needs authentication or guest workspace support.
- Identify the feature document to update under [Features/](Features/).

## Monetization Changes

- Update [15_Business_Monetization_Roadmap.md](15_Business_Monetization_Roadmap.md)
  when adding or changing pricing, plans, paid limits, checkout, affiliate
  links, analytics events, or B2B packaging.
- Keep paid usage limits enforceable in the backend, not only hidden in the UI.
- Keep local development usable without production payment credentials.
- Add clear upgrade states when a user reaches a plan limit.
- Add or update tests for entitlement checks and payment-webhook behavior.
- Include affiliate/sponsored-link disclosure near partner links and tracking
  events.

## API Changes

- Add or update DTOs in `StockAnalyzer.Application/DTOs`.
- Keep controllers thin.
- Add service abstraction and implementation in Application when orchestration is needed.
- Use typed `IMlServiceClient` calls for FastAPI interaction.
- Update [05_API_Documentation.md](05_API_Documentation.md).

## Database Changes

- Add domain entity or update existing entity.
- Add EF configuration and migration.
- Add repository abstraction/implementation if persistence is not already covered.
- Update [06_Database_Design.md](06_Database_Design.md).

## Frontend Changes

- Add route in `app.routes.ts` only when a new page is needed.
- Add typed interfaces to `core/models.ts`.
- Add API methods to `StockApiService`.
- Use signals for component state.
- Keep validation and error messages plain and actionable.

## AI Feature Changes

- OpenAI calls must remain inside `ml-service/app/ai/`.
- Use strict Pydantic schemas.
- Build context from StockAnalyzer data; do not allow arbitrary prompt injection.
- Keep deterministic fallback and metadata visible.
- Update [14_AI_Assistant_Instructions.md](14_AI_Assistant_Instructions.md) when guardrails change.
