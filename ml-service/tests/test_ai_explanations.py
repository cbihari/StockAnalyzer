from datetime import UTC, datetime
from types import SimpleNamespace

import pytest

from app.ai.deterministic_provider import DeterministicProvider
from app.ai import explanation_service, research_service
from app.ai.explanation_service import ProviderSettings, context_hash, fallback_reason
from app.ai.openai_provider import OpenAiProvider
from app.ai.prompts import PROMPT_VERSION
from app.ai.schemas import (
    DISCLAIMER,
    ResearchCitation,
    StockAiExplanation,
    StockAnalysisContext,
    StockResearchAnswer,
)


def context() -> StockAnalysisContext:
    return StockAnalysisContext(
        ticker="AAPL",
        company_name="Apple Inc.",
        latest_price=200,
        daily_change=2,
        daily_change_percent=0.01,
        prediction="UP",
        confidence=72,
        probability_up=0.72,
        probability_down=0.28,
        prediction_type="ml",
        model="RandomForestClassifier",
        model_status="existing_model",
        model_accuracy=0.67,
        rsi_14=55,
        sma_20=198,
        sma_50=190,
        ema_20=199,
        ema_50=192,
        macd=2.1,
        macd_signal=1.4,
        bollinger_upper=210,
        bollinger_lower=180,
        volume=100000,
        volume_change=0.1,
        trend="BULLISH",
        support=185,
        resistance=210,
        news_sentiment="NEUTRAL",
        news_sentiment_score=0,
        news_titles=["Apple publishes quarterly results"],
        timestamp=datetime(2026, 6, 12, tzinfo=UTC),
        data_period="1y",
        data_limitations=[],
    )


def explanation() -> StockAiExplanation:
    return StockAiExplanation(
        ticker="AAPL",
        prediction="UP",
        confidence=72,
        summary="The supplied indicators lean upward, with uncertainty.",
        supporting_signals=[],
        conflicting_signals=[],
        risk_level="MEDIUM",
        risk_factors=[],
        what_could_change_the_view=[],
        beginner_explanation="The model leans up but is not certain.",
        data_limitations=[],
        disclaimer=DISCLAIMER,
    )


def research_answer(question: str = "Why is the prediction up?") -> StockResearchAnswer:
    return StockResearchAnswer(
        ticker="AAPL",
        question=question,
        answer="The supplied trend and momentum evidence lean upward, with uncertainty.",
        key_points=["EMA20 is above EMA50."],
        citations=[ResearchCitation(
            source="technical_indicators",
            label="EMA trend",
            evidence="EMA20 199; EMA50 192.",
            observed_at=datetime(2026, 6, 12, tzinfo=UTC),
        )],
        limitations=[],
        follow_up_questions=["What are the main risks?"],
        disclaimer=DISCLAIMER,
    )


class FakeResponses:
    def __init__(self, parsed: StockAiExplanation | None):
        self.parsed = parsed
        self.kwargs = None

    async def parse(self, **kwargs):
        self.kwargs = kwargs
        return SimpleNamespace(
            output_parsed=self.parsed,
            usage=SimpleNamespace(input_tokens=123, output_tokens=45),
        )


@pytest.mark.asyncio
async def test_openai_provider_uses_structured_response_and_grounded_context():
    responses = FakeResponses(explanation())
    provider = OpenAiProvider("test-key", "gpt-5.5", "low", 30, SimpleNamespace(responses=responses))

    result = await provider.generate_stock_explanation(context())

    assert result.ticker == "AAPL"
    assert responses.kwargs["text_format"] is StockAiExplanation
    assert "Apple publishes quarterly results" in responses.kwargs["input"]
    assert responses.kwargs["store"] is False
    assert provider.input_tokens == 123
    assert provider.output_tokens == 45


@pytest.mark.asyncio
async def test_openai_provider_rejects_refusal_or_unparsed_response():
    provider = OpenAiProvider("test-key", "gpt-5.5", "low", 30, SimpleNamespace(responses=FakeResponses(None)))
    with pytest.raises(ValueError):
        await provider.generate_stock_explanation(context())


@pytest.mark.asyncio
async def test_openai_provider_rejects_changed_grounding_fields():
    changed = explanation().model_copy(update={"confidence": 99})
    provider = OpenAiProvider("test-key", "gpt-5.5", "low", 30, SimpleNamespace(responses=FakeResponses(changed)))
    with pytest.raises(ValueError):
        await provider.generate_stock_explanation(context())


@pytest.mark.asyncio
async def test_openai_provider_returns_structured_research_answer():
    responses = FakeResponses(research_answer())
    provider = OpenAiProvider("test-key", "gpt-5.5", "low", 30, SimpleNamespace(responses=responses))

    result = await provider.answer_research_question(context(), "Why is the prediction up?")

    assert result.ticker == "AAPL"
    assert responses.kwargs["text_format"] is StockResearchAnswer
    assert "USER_QUESTION" in responses.kwargs["input"]
    assert "Apple publishes quarterly results" in responses.kwargs["input"]


@pytest.mark.asyncio
async def test_deterministic_research_answer_refuses_trading_advice():
    result = await DeterministicProvider().answer_research_question(
        context(),
        "Should I buy AAPL now?",
    )

    assert "cannot decide" in result.answer.lower()
    assert result.citations
    assert result.disclaimer == DISCLAIMER


@pytest.mark.asyncio
async def test_deterministic_research_prioritizes_prediction_intent_over_model_keyword():
    result = await DeterministicProvider().answer_research_question(
        context(),
        "Why does the model lean in this direction, and what evidence conflicts with it?",
    )

    assert "technical trend" in result.answer.lower()
    assert "recorded holdout accuracy" not in result.answer.lower()
    assert any("EMA20" in point for point in result.key_points)


@pytest.mark.asyncio
async def test_deterministic_provider_preserves_grounding_and_disclaimer():
    result = await DeterministicProvider().generate_stock_explanation(context())
    assert result.disclaimer == DISCLAIMER
    assert any(signal.signal == "EMA trend" for signal in result.supporting_signals)
    assert "buy" not in result.summary.lower()
    assert result.confidence == context().confidence


def test_context_hash_includes_model_and_prompt_version_but_is_stable():
    first = context_hash(context(), "gpt-5.5")
    assert first == context_hash(context(), "gpt-5.5")
    assert first != context_hash(context(), "another-model")
    assert PROMPT_VERSION == "stock-research-v1"


def test_provider_settings_default_to_openai(monkeypatch):
    monkeypatch.delenv("LLM_PROVIDER", raising=False)
    monkeypatch.delenv("OPENAI_MODEL", raising=False)
    settings = ProviderSettings.from_environment()
    assert settings.provider == "openai"
    assert settings.model == "gpt-5.5"


def test_provider_settings_reads_api_key_from_secret_file(monkeypatch, tmp_path):
    secret = tmp_path / "openai_api_key"
    secret.write_text("test-secret-key\n", encoding="utf-8")
    monkeypatch.delenv("OPENAI_API_KEY", raising=False)
    monkeypatch.setenv("OPENAI_API_KEY_FILE", str(secret))

    settings = ProviderSettings.from_environment()

    assert settings.api_key == "test-secret-key"


def test_timeout_and_rate_limit_are_safe_fallback_reasons():
    class RateLimitError(Exception):
        pass

    assert fallback_reason(TimeoutError()) == "timeout"
    assert fallback_reason(RateLimitError()) == "rate_limited"


def test_incomplete_structured_response_is_rejected():
    with pytest.raises(Exception):
        StockAiExplanation.model_validate({"ticker": "AAPL", "prediction": "UP"})


@pytest.mark.asyncio
async def test_deterministic_explanation_cache(monkeypatch):
    async def fake_context(_ticker: str):
        return context()

    explanation_service._cache.clear()
    monkeypatch.setattr(explanation_service, "build_stock_analysis_context", fake_context)
    monkeypatch.setenv("AI_EXPLANATIONS_ENABLED", "false")
    first = await explanation_service.generate_explanation("AAPL", False)
    second = await explanation_service.generate_explanation("AAPL", False)
    assert first.cached is False
    assert second.cached is True


@pytest.mark.asyncio
async def test_deterministic_research_cache(monkeypatch):
    async def fake_context(_ticker: str):
        return context()

    research_service._cache.clear()
    monkeypatch.setattr(research_service, "build_stock_analysis_context", fake_context)
    monkeypatch.setenv("AI_EXPLANATIONS_ENABLED", "false")
    first = await research_service.answer_research_question("AAPL", "Explain RSI")
    second = await research_service.answer_research_question("AAPL", "Explain RSI")
    assert first.cached is False
    assert second.cached is True
    assert first.provider == "deterministic"
