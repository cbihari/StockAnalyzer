# Business Domain

StockAnalyzer serves retail learners who want to understand stock movement signals without treating the product as investment advice.

## Core Concepts

| Concept | Meaning |
| --- | --- |
| Ticker | Exchange/provider symbol accepted by Yahoo Finance, such as `RELIANCE.NS`, `TCS.NS`, `AAPL`, or `^NSEI`. |
| Stock analysis | Unified research snapshot containing quote, history, indicators, risk context, prediction, and model metadata. |
| Prediction | Educational next-trading-day `UP` or `DOWN` estimate with confidence and optional model probabilities. |
| Rule-based fallback | Deterministic technical-indicator prediction used when trained ML cannot be used. |
| Model metric | Accuracy, precision, recall, confusion matrix, and feature importance for ticker-specific Random Forest models. |
| Guest workspace | Browser-scoped saved watchlist, alerts, notifications, and portfolio stored locally and synced to PostgreSQL by `X-Client-ID`. |
| AI explanation | Grounded, structured explanation generated or deterministically produced from StockAnalyzer evidence. |
| AI research assistant | Ticker-scoped Q&A over the app's prepared market snapshot and citations. |

## Business Rules

- The product must state that outputs are educational and not financial advice.
- Monetization must sell research workflow, education, alerts, exports, and
  productivity; it must not sell personalized recommendations or stock tips.
- Paid features must not introduce buy/sell/hold calls, target prices,
  guaranteed-return claims, or model-confidence-as-profit-probability language.
- Broker or partner monetization must include visible affiliate/sponsored-link
  disclosure near the relevant action.
- Use company-name-first, plain-language presentation where possible, especially for Indian beginner investors.
- Prefer no news over unrelated news; source relevance is a trust boundary.
- Tickers are normalized to uppercase and validated before backend/ML calls.
- Predictions must be auditable through persisted prediction history and later outcome evaluation.
- First-time model use may train a model during the request; UI must handle long-running and fallback states.
- OpenAI access is allowed only inside the FastAPI service. Angular and .NET must never receive or expose `OPENAI_API_KEY`.
- AI features must remain grounded in StockAnalyzer evidence and must not accept arbitrary open-ended prompt control.

## Primary User Journeys

1. Search for a company or ticker.
2. Open a stock detail page.
3. Review quote, price trend, indicators, directional estimate, risks, model status, news, and optional AI explanation.
4. Save stocks to a watchlist, configure alerts, or add holdings to a manual portfolio.
5. Review prediction history and model accuracy to understand reliability.
6. Optionally create an account and claim the browser workspace.

## Commercial Direction

StockAnalyzer's first commercial path should be a freemium educational research
SaaS:

- Free users receive enough analysis to trust the product.
- Pro users pay for higher limits, AI explanation usage, alerts, portfolio
  insights, comparison, and exports.
- Power users or creators pay for higher usage, presentation/export features,
  and research-productivity tooling.

See [15_Business_Monetization_Roadmap.md](15_Business_Monetization_Roadmap.md)
for the full business plan, packaging, and rollout phases.

See feature-level flows in [Features/](Features/).
