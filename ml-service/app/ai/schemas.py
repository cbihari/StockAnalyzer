from datetime import datetime
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, field_validator


DISCLAIMER = "Educational purpose only. Not financial advice."


class StrictModel(BaseModel):
    model_config = ConfigDict(extra="forbid")


class ExplanationSignal(StrictModel):
    signal: str
    explanation: str
    importance: Literal["low", "medium", "high"]


class StockAiExplanation(StrictModel):
    ticker: str
    prediction: Literal["UP", "DOWN"]
    confidence: int = Field(ge=0, le=100)
    summary: str
    supporting_signals: list[ExplanationSignal]
    conflicting_signals: list[ExplanationSignal]
    risk_level: Literal["LOW", "MEDIUM", "HIGH"]
    risk_factors: list[str]
    what_could_change_the_view: list[str]
    beginner_explanation: str
    data_limitations: list[str]
    disclaimer: str

    @field_validator("disclaimer")
    @classmethod
    def exact_disclaimer(cls, value: str) -> str:
        if value != DISCLAIMER:
            raise ValueError("disclaimer must match the required educational disclaimer")
        return value


class StockAnalysisContext(StrictModel):
    ticker: str
    company_name: str | None
    latest_price: float
    daily_change: float
    daily_change_percent: float
    prediction: Literal["UP", "DOWN"]
    confidence: int
    probability_up: float
    probability_down: float
    prediction_type: str
    model: str
    model_status: str
    model_accuracy: float | None
    rsi_14: float | None
    sma_20: float | None
    sma_50: float | None
    ema_20: float | None
    ema_50: float | None
    macd: float | None
    macd_signal: float | None
    bollinger_upper: float | None
    bollinger_lower: float | None
    volume: int
    volume_change: float | None
    trend: Literal["BULLISH", "BEARISH", "DATA_LIMITED"]
    support: float | None
    resistance: float | None
    news_sentiment: Literal["POSITIVE", "NEUTRAL", "NEGATIVE", "UNAVAILABLE"]
    news_sentiment_score: float | None
    news_titles: list[str]
    timestamp: datetime
    data_period: str
    data_limitations: list[str]


class AiExplainRequest(StrictModel):
    ticker: str = Field(min_length=1, max_length=20, pattern=r"^[A-Za-z0-9.^=\-]+$")
    force_refresh: bool = False

    @field_validator("ticker")
    @classmethod
    def normalize_ticker(cls, value: str) -> str:
        return value.strip().upper()


class AiExplainResponse(StrictModel):
    ticker: str
    explanation: StockAiExplanation
    provider: Literal["openai", "deterministic"]
    model: str
    fallback_used: bool
    fallback_reason: str | None
    generated_at: datetime
    cached: bool
    input_hash: str
    prediction_type: str
    prompt_version: str
    input_tokens: int | None
    output_tokens: int | None


class ResearchCitation(StrictModel):
    source: Literal[
        "prediction",
        "technical_indicators",
        "market_context",
        "news_sentiment",
        "model_performance",
    ]
    label: str
    evidence: str
    observed_at: datetime


class StockResearchAnswer(StrictModel):
    ticker: str
    question: str
    answer: str
    key_points: list[str]
    citations: list[ResearchCitation]
    limitations: list[str]
    follow_up_questions: list[str]
    disclaimer: str

    @field_validator("disclaimer")
    @classmethod
    def exact_research_disclaimer(cls, value: str) -> str:
        if value != DISCLAIMER:
            raise ValueError("disclaimer must match the required educational disclaimer")
        return value


class AiResearchRequest(StrictModel):
    ticker: str = Field(min_length=1, max_length=20, pattern=r"^[A-Za-z0-9.^=\-]+$")
    question: str = Field(min_length=3, max_length=300)

    @field_validator("ticker")
    @classmethod
    def normalize_research_ticker(cls, value: str) -> str:
        return value.strip().upper()

    @field_validator("question")
    @classmethod
    def normalize_question(cls, value: str) -> str:
        normalized = " ".join(value.split())
        if any(ord(character) < 32 for character in normalized):
            raise ValueError("question contains unsupported control characters")
        return normalized


class AiResearchResponse(StrictModel):
    ticker: str
    answer: StockResearchAnswer
    provider: Literal["openai", "deterministic"]
    model: str
    fallback_used: bool
    fallback_reason: str | None
    generated_at: datetime
    cached: bool
    input_tokens: int | None
    output_tokens: int | None
