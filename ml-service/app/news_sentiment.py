import logging
import re
import time
from datetime import UTC, datetime, timedelta
from hashlib import sha256
from typing import Annotated, Literal

from fastapi import APIRouter, HTTPException, Query
from pydantic import BaseModel, HttpUrl

from app.market_data_provider import MARKET_DATA_PROVIDER
from app.model_registry import normalize_ticker

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/stocks", tags=["stocks"])

POSITIVE_WORDS = {
    "beat", "beats", "growth", "gain", "gains", "higher", "profit", "profits",
    "record", "raise", "raises", "raised", "upgrade", "upgraded", "strong",
    "surge", "surges", "expands", "expansion", "approval", "approved", "wins",
}
NEGATIVE_WORDS = {
    "miss", "misses", "decline", "declines", "lower", "loss", "losses", "cuts",
    "cut", "downgrade", "downgraded", "weak", "falls", "fall", "drops", "drop",
    "probe", "lawsuit", "recall", "risk", "warning", "layoffs", "fraud", "bad",
    "concern", "concerns", "disappointing",
}
TOPICS = {
    "Earnings": {"earnings", "revenue", "profit", "quarter", "results", "eps"},
    "Guidance": {"guidance", "forecast", "outlook", "expects", "target"},
    "Regulation": {"regulator", "regulation", "probe", "lawsuit", "court", "sec", "fine"},
    "Product": {"product", "launch", "launches", "launched", "unveils", "unveiled", "feature", "features", "chip", "service", "platform", "approval"},
    "Management": {"ceo", "cfo", "executive", "management", "resigns", "appoints"},
    "Analyst commentary": {"analyst", "upgrade", "downgrade", "rating", "price target"},
    "Corporate action": {"acquisition", "merger", "buyback", "dividend", "split", "stake"},
}
HIGH_IMPACT_WORDS = {
    "earnings", "guidance", "acquisition", "merger", "lawsuit", "probe", "recall",
    "ceo", "fraud", "approval", "bankruptcy",
}
MEDIUM_IMPACT_WORDS = {"analyst", "upgrade", "downgrade", "launch", "dividend", "buyback", "forecast"}
WHY_TOPIC_MATTERS = {
    "Earnings": "Earnings can reset expectations for revenue, profitability, and valuation.",
    "Guidance": "Forward guidance can change how investors estimate future business performance.",
    "Regulation": "Regulatory events may affect costs, operations, reputation, or market access.",
    "Product": "Product developments may influence demand, competition, and future revenue.",
    "Management": "Leadership changes can affect strategy and confidence in execution.",
    "Analyst commentary": "Analyst opinions can influence short-term attention but are not company results.",
    "Corporate action": "Corporate actions can alter capital structure, ownership, or growth expectations.",
    "General": "This may affect market expectations, but the headline alone does not establish price direction.",
}
GENERIC_COMPANY_WORDS = {
    "inc", "incorporated", "limited", "ltd", "corporation", "corp", "company", "holdings",
    "group", "class", "common", "stock", "technologies", "technology", "industries", "bank",
}


class NewsArticle(BaseModel):
    id: str
    headline: str
    publisher: str
    published_at: datetime
    url: HttpUrl
    sentiment: Literal["POSITIVE", "NEUTRAL", "NEGATIVE"]
    sentiment_score: float
    impact: Literal["HIGH", "MEDIUM", "LOW"]
    topic: str
    summary: str
    why_it_matters: str


class StockNewsResponse(BaseModel):
    ticker: str
    overall_sentiment: Literal["POSITIVE", "NEUTRAL", "NEGATIVE"]
    sentiment_score: float
    confidence: float
    coverage: Literal["HIGH", "MEDIUM", "LOW", "NONE"]
    article_count: int
    lookback_days: int
    highest_impact_topic: str | None
    positive_count: int
    neutral_count: int
    negative_count: int
    articles: list[NewsArticle]
    as_of: datetime
    data_source: str
    methodology: str
    warning: str


_cache: dict[tuple[str, int, int], tuple[float, StockNewsResponse]] = {}
CACHE_SECONDS = 900


def _tokens(text: str) -> set[str]:
    return set(re.findall(r"[a-z]+", text.casefold()))


def classify_sentiment(text: str) -> tuple[Literal["POSITIVE", "NEUTRAL", "NEGATIVE"], float]:
    words = _tokens(text)
    positive = len(words & POSITIVE_WORDS)
    negative = len(words & NEGATIVE_WORDS)
    score = round(max(-1.0, min(1.0, (positive - negative) / max(positive + negative, 2))), 2)
    label: Literal["POSITIVE", "NEUTRAL", "NEGATIVE"] = (
        "POSITIVE" if score >= 0.18 else "NEGATIVE" if score <= -0.18 else "NEUTRAL"
    )
    return label, score


def classify_topic(text: str) -> str:
    normalized = text.casefold()
    words = _tokens(text)
    for topic, keywords in TOPICS.items():
        if any(keyword in normalized if " " in keyword else keyword in words for keyword in keywords):
            return topic
    return "General"


def classify_impact(text: str, published_at: datetime, now: datetime) -> Literal["HIGH", "MEDIUM", "LOW"]:
    words = _tokens(text)
    age = now - published_at
    if words & HIGH_IMPACT_WORDS and age <= timedelta(days=7):
        return "HIGH"
    if words & (HIGH_IMPACT_WORDS | MEDIUM_IMPACT_WORDS) or age <= timedelta(days=2):
        return "MEDIUM"
    return "LOW"


def _published_at(value: object, now: datetime) -> datetime:
    if isinstance(value, (int, float)):
        return datetime.fromtimestamp(value, UTC)
    if isinstance(value, str):
        try:
            parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
            return parsed if parsed.tzinfo else parsed.replace(tzinfo=UTC)
        except ValueError:
            pass
    return now


def _url(content: dict[str, object]) -> str:
    for key in ("canonicalUrl", "clickThroughUrl"):
        candidate = content.get(key)
        if isinstance(candidate, dict) and candidate.get("url"):
            return str(candidate["url"])
    return str(content.get("link") or content.get("url") or "")


def normalize_article(raw: dict[str, object], now: datetime) -> NewsArticle | None:
    nested = raw.get("content")
    content = nested if isinstance(nested, dict) else raw
    headline = str(content.get("title") or content.get("headline") or "").strip()
    url = _url(content)
    if not headline or not url.startswith(("http://", "https://")):
        return None
    provider = content.get("provider")
    publisher = str(provider.get("displayName") if isinstance(provider, dict) else provider or content.get("publisher") or "Unknown source")
    published_at = _published_at(content.get("pubDate") or content.get("providerPublishTime"), now)
    description = str(content.get("summary") or content.get("description") or "").strip()
    evidence = f"{headline}. {description}"
    sentiment, score = classify_sentiment(evidence)
    topic = classify_topic(evidence)
    return NewsArticle(
        id=sha256(f"{headline}|{url}".encode()).hexdigest()[:16],
        headline=headline,
        publisher=publisher,
        published_at=published_at,
        url=url,
        sentiment=sentiment,
        sentiment_score=score,
        impact=classify_impact(evidence, published_at, now),
        topic=topic,
        summary=description[:320] if description else "No publisher summary was available for this headline.",
        why_it_matters=WHY_TOPIC_MATTERS[topic],
    )


def relevance_terms(ticker: str) -> set[str]:
    base_symbol = ticker.split(".", 1)[0].casefold()
    terms = {base_symbol}
    try:
        quotes = MARKET_DATA_PROVIDER.search(ticker, 3)
    except Exception:
        logger.warning("Company-name lookup unavailable ticker=%s", ticker, exc_info=True)
        return terms
    exact = next((quote for quote in quotes if str(quote.get("symbol") or "").upper() == ticker), None)
    if exact is None:
        return terms
    name = str(exact.get("longname") or exact.get("shortname") or "").casefold()
    meaningful = [word for word in _tokens(name) if len(word) >= 4 and word not in GENERIC_COMPANY_WORDS]
    if meaningful:
        terms.add(meaningful[0])
        terms.add(" ".join(meaningful[:3]))
    return terms


def is_relevant(content: dict[str, object], terms: set[str]) -> bool:
    text = f"{content.get('title') or ''} {content.get('summary') or content.get('description') or ''}".casefold()
    words = _tokens(text)
    return any(term in words if " " not in term else term in text for term in terms)


def build_stock_news(ticker: str, lookback_days: int, limit: int) -> StockNewsResponse:
    normalized = normalize_ticker(ticker)
    cache_key = (normalized, lookback_days, limit)
    cached = _cache.get(cache_key)
    if cached and time.monotonic() - cached[0] < CACHE_SECONDS:
        return cached[1]

    now = datetime.now(UTC)
    try:
        raw_articles = MARKET_DATA_PROVIDER.news(normalized, max(limit * 2, 20))
    except Exception as exc:
        logger.exception("News provider failed ticker=%s", normalized)
        raise HTTPException(status_code=502, detail="Stock news is temporarily unavailable from the configured provider.") from exc

    cutoff = now - timedelta(days=lookback_days)
    terms = relevance_terms(normalized)
    articles: list[NewsArticle] = []
    seen: set[str] = set()
    for raw in raw_articles:
        if not isinstance(raw, dict):
            continue
        nested = raw.get("content")
        content = nested if isinstance(nested, dict) else raw
        if not is_relevant(content, terms):
            continue
        article = normalize_article(raw, now)
        if article is None or article.published_at < cutoff or article.headline.casefold() in seen:
            continue
        seen.add(article.headline.casefold())
        articles.append(article)
    articles.sort(key=lambda article: article.published_at, reverse=True)
    articles = articles[:limit]

    count = len(articles)
    score = round(sum(article.sentiment_score for article in articles) / count, 2) if count else 0.0
    overall: Literal["POSITIVE", "NEUTRAL", "NEGATIVE"] = (
        "POSITIVE" if score >= 0.15 else "NEGATIVE" if score <= -0.15 else "NEUTRAL"
    )
    coverage: Literal["HIGH", "MEDIUM", "LOW", "NONE"] = (
        "HIGH" if count >= 8 else "MEDIUM" if count >= 4 else "LOW" if count else "NONE"
    )
    confidence = round(min(0.9, 0.2 + count * 0.08), 2) if count else 0.0
    highest = next((article.topic for article in articles if article.impact == "HIGH"), articles[0].topic if articles else None)
    warning = (
        "No recent articles were returned. Technical analysis remains available."
        if not count else
        "Low news coverage: treat the sentiment summary as incomplete."
        if coverage == "LOW" else
        "News sentiment describes headline tone, not expected stock-price direction."
    )
    response = StockNewsResponse(
        ticker=normalized,
        overall_sentiment=overall,
        sentiment_score=score,
        confidence=confidence,
        coverage=coverage,
        article_count=count,
        lookback_days=lookback_days,
        highest_impact_topic=highest,
        positive_count=sum(article.sentiment == "POSITIVE" for article in articles),
        neutral_count=sum(article.sentiment == "NEUTRAL" for article in articles),
        negative_count=sum(article.sentiment == "NEGATIVE" for article in articles),
        articles=articles,
        as_of=now,
        data_source=MARKET_DATA_PROVIDER.name,
        methodology="Deterministic keyword scoring over provider headlines and summaries; no LLM is used.",
        warning=warning,
    )
    _cache[cache_key] = (time.monotonic(), response)
    return response


@router.get("/news", response_model=StockNewsResponse)
def get_stock_news(
    ticker: Annotated[str, Query(min_length=1, max_length=20, pattern=r"^[A-Za-z0-9.^=\-]+$")],
    lookback_days: Annotated[int, Query(ge=1, le=30)] = 7,
    limit: Annotated[int, Query(ge=1, le=20)] = 10,
) -> StockNewsResponse:
    return build_stock_news(ticker, lookback_days, limit)
