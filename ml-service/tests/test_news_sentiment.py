from datetime import UTC, datetime

from app import news_sentiment


def test_build_stock_news_deduplicates_and_scores_articles(monkeypatch) -> None:
    now = datetime.now(UTC).isoformat()
    raw = [
        {"content": {"title": "Apple beats earnings estimates with record profit", "summary": "AAPL revenue growth remained strong.", "pubDate": now, "provider": {"displayName": "Test Wire"}, "canonicalUrl": {"url": "https://example.com/good"}}},
        {"content": {"title": "Apple beats earnings estimates with record profit", "summary": "Duplicate.", "pubDate": now, "provider": {"displayName": "Test Wire"}, "canonicalUrl": {"url": "https://example.com/duplicate"}}},
        {"content": {"title": "Regulator opens Apple probe and warns company", "summary": "A new AAPL regulatory risk was disclosed.", "pubDate": now, "provider": {"displayName": "Test Wire"}, "canonicalUrl": {"url": "https://example.com/bad"}}},
        {"content": {"title": "Unrelated company announces product", "summary": "No matching symbol.", "pubDate": now, "provider": {"displayName": "Test Wire"}, "canonicalUrl": {"url": "https://example.com/unrelated"}}},
    ]
    monkeypatch.setattr(news_sentiment.MARKET_DATA_PROVIDER, "news", lambda ticker, limit: raw)
    monkeypatch.setattr(news_sentiment.MARKET_DATA_PROVIDER, "search", lambda ticker, limit: [{"symbol": "AAPL", "longname": "Apple Inc."}])
    news_sentiment._cache.clear()

    result = news_sentiment.build_stock_news("aapl", 7, 10)

    assert result.ticker == "AAPL"
    assert result.article_count == 2
    assert result.positive_count == 1
    assert result.negative_count == 1
    assert result.coverage == "LOW"
    assert result.articles[0].publisher == "Test Wire"
    assert result.articles[0].impact == "HIGH"


def test_classification_keeps_unmatched_headline_neutral() -> None:
    label, score = news_sentiment.classify_sentiment("Company schedules annual shareholder meeting")

    assert label == "NEUTRAL"
    assert score == 0
