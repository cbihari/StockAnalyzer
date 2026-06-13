# STOCKANALYZER_CONTEXT.md

## Project Name

StockAnalyzer

## Product Vision

StockAnalyzer is an AI-powered stock research platform.

The goal is not to provide financial advice or guaranteed stock predictions.

The goal is to help users:

- Research stocks
- Understand technical indicators
- Understand model outputs
- Evaluate risks
- Compare opportunities
- Learn investing concepts
- Make informed decisions

The platform emphasizes:

- Transparency
- Education
- Explainability
- Evidence
- Risk awareness

-----

## Product Positioning

Do NOT describe the product as:
“Stock Predictor”

Prefer:

- AI Stock Research Platform
- Explainable AI Stock Analyst
- AI Stock Research Workspace

## Core Philosophy

Always show:

- Supporting evidence
- Conflicting evidence
- Confidence score
- Risk level
- Model transparency

Never claim certainty.
Always communicate uncertainty.
Educational use only.
Not financial advice.

-----

## Target Users

1. Beginner Investors
1. Swing Traders
1. Long-Term Investors
1. Retail Traders
1. Stock Market Learners
1. Finance Content Creators

-----

## Technology Stack

**Frontend:**

- Angular
- TypeScript
- SCSS
- Chart.js

**Backend:**

- .NET 9 Web API
- C#
- Entity Framework Core

**ML Service:**

- Python
- FastAPI
- Pandas
- NumPy
- Scikit-Learn
- yfinance

**Database:**

- PostgreSQL

**Deployment:**

- Angular → Vercel
- Backend → Render
- ML Service → Render
- Database → Neon

-----

## Existing Features

### Home Page

Landing page introducing StockAnalyzer.
Includes “How this works” hint guiding new users:
Home → Research (search ticker) → Dashboard → Learn (if confused by terms).

### Research Dashboard

Users enter:

- RELIANCE.NS
- AAPL
- MSFT
- TCS.NS

Displays:

- Price
- Daily Change
- Trend
- Prediction
- Confidence
- Risk
- Technical Indicators
- Supporting Signals
- Conflicting Signals
- AI Explanation

Includes affiliate disclosure note (see Monetization section) below AI explanation.

### Market Dashboard

Displays:

India:

- NIFTY50
- BANKNIFTY
- SENSEX

US:

- Major indices

Includes:

- Market Breadth
- Top Gainers
- Top Losers
- Most Active Stocks

### Stock Comparison

Compare:

- 2 or 3 stocks (3rd stock is a Pro feature, see Monetization)

Compare:

- Price performance
- Prediction
- Confidence
- Risk
- Technical indicators
- Model reliability

Includes affiliate disclosure note below comparison results.

### AI Research Assistant

Users ask questions such as:

- Why is the model bullish?
- What risks exist?
- Which indicators support the signal?
- Explain RSI.
- Explain MACD.

Responses must remain educational.
No buy/sell recommendations.
Free tier: 3 questions/day. Pro tier: unlimited.

### Watchlist

Track selected stocks.

Features:

- Price updates
- Quick research access
- Alerts (Pro: WhatsApp/email)
- Notes

Free tier: 5 stocks. Pro tier: 50 stocks.

### Portfolio Tracker

Users manually enter:

- Ticker
- Quantity
- Average Cost
- Purchase Date
- Notes

Features:

- Portfolio value
- Allocation
- Concentration
- Performance

No brokerage integration currently. Free for all users.

### Model Performance Center

Shows:

- Accuracy
- Precision
- Recall
- Confusion Matrix
- Training Details

Purpose: Model transparency.

### Prediction History

Tracks:

- Prediction
- Confidence
- Actual Outcome

Purpose: Prediction accountability. Free for all users.

### Learning Center

Topics:

- RSI
- MACD
- EMA
- SMA
- Volume
- Risk Management
- Support & Resistance
- Confidence Scores

Purpose: Teach investing concepts. Free for all users — primary SEO/acquisition surface.

### About Page

Explains the “why” behind StockAnalyzer: solo-built, explainability-first,
not financial advice, feedback-driven development.

-----

## UI Design System

**Style:**

- Futuristic
- Premium SaaS
- FinTech Dashboard

**Visual Design:**

- Dark mode first
- Glassmorphism
- Neon green accents
- Smooth gradients
- Rounded cards
- Soft shadows

**Animations:**

- Smooth transitions
- Hover effects
- Loading skeletons
- Page transitions

**Responsive:**

- Mobile First
- Tablet
- Desktop

**Clarity additions:**

- Reusable `<app-info-tip>` component for hover/tap explanations of technical
  terms (RSI, MACD, EMA20, EMA50, SMA20, SMA50, Confidence, Volatility,
  Support, Resistance, Risk Level, Model Accuracy).
- Tooltip copy must be under 15 words, plain language.
- Input placeholders use plain-language examples
  (e.g. “Number of shares you own” instead of “Quantity”).

-----

## Monetization Strategy

### Tiers

**Free:**

- 5 watchlist stocks
- 3 AI Assistant questions/day
- Daily-refresh predictions
- 2-stock compare
- Full Learning Center access
- Full Portfolio Tracker
- Full Prediction History

**Pro (₹149/month or ₹1499/year):**

- 50 watchlist stocks
- Unlimited AI Assistant questions
- On-demand model refresh
- 3-stock compare
- Price alerts via WhatsApp/email
- CSV export (Watchlist, Portfolio, History)

Upgrade page is UI-only until payment integration (Razorpay) is added.
Use `<app-tier-badge>` component to mark Free/Pro features once gating begins.

### Affiliate Program

`<app-affiliate-note>` component shown on Research Dashboard and Compare pages only.

Copy: “This analysis is for education only and isn’t investment advice.
Open a free demat account with our partner broker — StockAnalyzer may earn
a referral fee at no extra cost to you.”

Style: small, muted text with top border separator. Must not resemble an ad banner.
Partner links (Zerodha/Groww/Upstox affiliate IDs) to be inserted once registered.

### Out of Scope (avoid)

- Display ads (conflicts with premium aesthetic, slows UI)
- Any framing as “signal selling” or trade tips (SEBI compliance risk)

-----

## Coding Standards

**Frontend:**

- Reusable Components
- Angular Best Practices
- Responsive Design

**Backend:**

- Clean Architecture
- Dependency Injection
- DTO Pattern
- Repository Pattern

**Python:**

- Modular
- Typed
- Logged
- Error Handling

-----

## Rules For AI Assistants

1. Do not break existing APIs.
1. Do not remove existing features.
1. Preserve backward compatibility.
1. Prefer reusable components.
1. Keep responsive design.
1. Keep deployment compatibility.
1. Avoid unnecessary dependencies.
1. Minimize changes.
1. Return only changed files unless requested otherwise.
1. Follow existing architecture.

-----

## Deployment

**Frontend:** Vercel
**Backend:** Render
**ML Service:** Render
**Database:** Neon PostgreSQL

**Environment Variables:**

Backend:

- DATABASE_URL
- ML_SERVICE_URL
- ALLOWED_ORIGINS
- ASPNETCORE_ENVIRONMENT

ML:

- PYTHON_ENV
- MODEL_DIR

-----

## Growth & Distribution

Primary channels (low/no budget):

- WhatsApp/Telegram trading & investing groups
- Reddit (r/IndianStockMarket, r/IndiaInvestments, r/DalalStreetTalks)
- Twitter/X — share Research Dashboard screenshots with reasoning
- Finance content creators — offer early access for unique content angles
- SEO via Learning Center articles (RSI/MACD/EMA explainers as blog posts
  linking back to the live tool)

Core retention loop: search → understand (Learning Center) → save to
Watchlist → return next day for updates.

-----

## Future Roadmap

**Phase 1:** Dynamic ticker autocomplete
**Phase 2:** Portfolio Screenshot Analyzer
**Phase 3:** News Sentiment Analysis
**Phase 4:** AI Portfolio Assistant
**Phase 5:** Mobile App
**Phase 6:** Subscription Plans (Razorpay integration, feature gating)
**Phase 7:** Advanced Research Workspace / Creator API access

-----

## Output Rules For Codex

Always:

- Return only changed files
- Do not rewrite entire project
- Do not output unchanged code
- Minimize token usage
- Preserve existing functionality
- Follow current architecture