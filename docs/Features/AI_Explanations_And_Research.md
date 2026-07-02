# AI Explanations And Research

## Purpose

Explain predictions and answer ticker-scoped research questions using structured, grounded StockAnalyzer evidence with deterministic fallback.

## Business Flow

1. User explicitly requests an AI explanation or asks a suggested research question.
2. .NET returns a valid cached explanation without consuming quota when one is available.
3. If no cache is available, .NET checks the `ai_explanation` plan quota before calling FastAPI.
4. .NET forwards a bounded ticker request to FastAPI only when quota remains.
5. FastAPI builds a grounded context from prediction, prices, indicators, metrics, support/resistance, and bounded news.
6. OpenAI is used only if configured; otherwise deterministic fallback returns safe educational output.
7. .NET stores explanation metadata/cache and records usage for newly generated responses.

## UI Flow

- Stock detail: AI Research Explanation panel with Generate/Regenerate and provider/fallback metadata.
- `/assistant`: ticker-scoped research assistant with suggested questions and citations.

## Backend Flow

- .NET: `PredictionsController.ExplainAi`, `ResearchController.Ask`, `AiExplanationService`, `AiResearchService`.
- FastAPI: `app/ai/router.py`, `context_builder.py`, `explanation_service.py`, `research_service.py`, providers, schemas, prompts.

## API Endpoints

- `POST /api/predictions/explain-ai/{ticker}?forceRefresh=false`
- `POST /api/research/{ticker}` with `{ "question": "..." }`
- FastAPI: `POST /ai/explain`
- FastAPI: `POST /ai/research`

## Models

- Frontend: `AiExplanationResponse`, `StockAiExplanation`, `AiResearchResponse`, `StockResearchAnswer`
- Backend: `AiExplanationResponseDto`, `AiResearchResponseDto`
- Domain: `AiExplanation`
- FastAPI: `AiExplainRequest`, `AiExplainResponse`, `AiResearchRequest`, `AiResearchResponse`

## Validation

- Ticker is normalized.
- Research question must stay short and ticker-scoped.
- Responses must match strict schemas.
- Provider/news content is untrusted and cannot override system guardrails.

## Database Tables

- `AiExplanations`

## Error Handling

- FastAPI rate limits by client/IP.
- .NET returns `402 Plan limit reached` when the current plan has no remaining `ai_explanation` quota.
- Missing key, timeout, refusal, invalid schema, or grounding failure returns deterministic fallback.
- Cache is bypassed only with `forceRefresh=true`.

## Future Improvements

- User-visible citation quality scoring.
- Richer source provenance once licensed news/data providers are added.
- Quota-aware frontend upgrade prompt on stock detail.
