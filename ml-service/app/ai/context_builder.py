import asyncio
from datetime import UTC, datetime, time

from app.indicators import calculate_indicators, to_indicator_values
from app.market_data_provider import MARKET_DATA_PROVIDER
from app.ml_prediction import get_ml_prediction
from app.news_sentiment import build_stock_news
from app.stock_history import fetch_stock_history
from app.ai.schemas import StockAnalysisContext


def _company_name(ticker: str) -> str | None:
    try:
        match = next(
            (item for item in MARKET_DATA_PROVIDER.search(ticker, 5) if str(item.get("symbol", "")).upper() == ticker),
            None,
        )
        return str(match.get("longname") or match.get("shortname")) if match else None
    except Exception:
        return None


def _build_context(ticker: str) -> StockAnalysisContext:
    normalized, period, history = fetch_stock_history(ticker, "1y")
    prediction = get_ml_prediction(normalized)
    dataset = calculate_indicators(history)
    indicators = to_indicator_values(dataset.index[-1], dataset.iloc[-1])
    latest = history.iloc[-1]
    previous = history.iloc[-2]
    latest_price = float(latest["Close"])
    previous_price = float(previous["Close"])
    daily_change = latest_price - previous_price
    recent = history.tail(min(60, len(history)))
    limitations: list[str] = []
    if prediction.model_accuracy is None:
        limitations.append("Model accuracy is unavailable.")
    if prediction.fallback_used:
        limitations.append("The prediction uses the deterministic rule-based fallback.")
    try:
        news = build_stock_news(normalized, 7, 5)
        news_sentiment = news.overall_sentiment
        news_score = news.sentiment_score
        news_titles = [article.headline[:240] for article in news.articles[:5]]
        if not news_titles:
            limitations.append("No recent relevant news titles were available.")
    except Exception:
        news_sentiment = "UNAVAILABLE"
        news_score = None
        news_titles = []
        limitations.append("Recent news sentiment was unavailable.")

    trend = "DATA_LIMITED"
    if indicators.ema_20 is not None and indicators.ema_50 is not None:
        trend = "BULLISH" if indicators.ema_20 >= indicators.ema_50 else "BEARISH"
    return StockAnalysisContext(
        ticker=normalized,
        company_name=_company_name(normalized),
        latest_price=round(latest_price, 4),
        daily_change=round(daily_change, 4),
        daily_change_percent=round(daily_change / previous_price, 6) if previous_price else 0,
        prediction=prediction.prediction,
        confidence=prediction.confidence,
        probability_up=prediction.probability_up,
        probability_down=prediction.probability_down,
        prediction_type=prediction.prediction_type,
        model=prediction.model,
        model_status=prediction.model_status,
        model_accuracy=prediction.model_accuracy,
        rsi_14=indicators.rsi_14,
        sma_20=indicators.sma_20,
        sma_50=indicators.sma_50,
        ema_20=indicators.ema_20,
        ema_50=indicators.ema_50,
        macd=indicators.macd,
        macd_signal=indicators.macd_signal,
        bollinger_upper=indicators.bollinger_upper,
        bollinger_lower=indicators.bollinger_lower,
        volume=int(latest["Volume"]),
        volume_change=indicators.volume_change,
        trend=trend,
        support=round(float(recent["Low"].min()), 4),
        resistance=round(float(recent["High"].max()), 4),
        news_sentiment=news_sentiment,
        news_sentiment_score=news_score,
        news_titles=news_titles,
        timestamp=datetime.combine(indicators.date, time.min, tzinfo=UTC),
        data_period=period,
        data_limitations=limitations,
    )


async def build_stock_analysis_context(ticker: str) -> StockAnalysisContext:
    return await asyncio.to_thread(_build_context, ticker)
