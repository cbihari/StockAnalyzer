# UI Guidelines

The frontend should feel like an educational research tool, not a trading terminal or marketing site.

## Product Principles

- Lead with company/ticker, current context, simple summary, and risk.
- Show model confidence, fallback status, data freshness, and disclaimers clearly.
- For Indian beginner users, prefer plain-language explanations and examples such as `RELIANCE.NS`, `TCS.NS`, and `INFY.NS`.
- Avoid implying buy/sell/hold advice.
- Prefer fewer relevant news items over noisy or unrelated headlines.

## Layout Patterns

- Page routes live in `frontend/src/app/pages/`.
- Shared controls and visualizations live in `frontend/src/app/shared/`.
- Use visible states for loading, empty, validation, error, stale/delayed data, fallback, and success.
- Keep charts readable on mobile and desktop.
- Maintain keyboard-friendly search/autocomplete behavior.

## Navigation

Canonical routes are documented in [08_State_Management.md](08_State_Management.md) and defined in `frontend/src/app/app.routes.ts`.

Important user paths:

- Home/search to stock detail.
- Stock detail to news, comparison, watchlist, portfolio, and AI explanation.
- Watchlist and portfolio to saved workspace.
- Auth callback to workspace claim prompt.
- Prediction history to accuracy/model pages.

## Copy Rules

- Use "estimate", "signal", "confidence", "risk", and "educational" instead of directive trading language.
- Always preserve not-financial-advice framing for predictions and AI.
- Explain fallback behavior as a product safety feature, not a hidden failure.
