import hashlib
import json
import logging
import time
from datetime import UTC, datetime

from app.ai.context_builder import build_stock_analysis_context
from app.ai.deterministic_provider import DeterministicProvider
from app.ai.explanation_service import CACHE_SECONDS, ProviderSettings, fallback_reason
from app.ai.openai_provider import OpenAiProvider
from app.ai.prompts import ASSISTANT_PROMPT_VERSION
from app.ai.schemas import AiResearchResponse, StockAnalysisContext

logger = logging.getLogger(__name__)
_cache: dict[str, tuple[float, AiResearchResponse]] = {}


def research_hash(context: StockAnalysisContext, question: str, model: str) -> str:
    payload = {
        "context": context.model_dump(mode="json"),
        "question": question,
        "model": model,
        "prompt_version": ASSISTANT_PROMPT_VERSION,
    }
    return hashlib.sha256(json.dumps(payload, sort_keys=True, separators=(",", ":")).encode()).hexdigest()


async def answer_research_question(ticker: str, question: str) -> AiResearchResponse:
    started = time.perf_counter()
    settings = ProviderSettings.from_environment()
    context = await build_stock_analysis_context(ticker)
    requested_model = settings.model if settings.provider == "openai" else "deterministic-v1"
    cache_key = research_hash(context, question, requested_model)
    cached = _cache.get(cache_key)
    if cached and time.monotonic() - cached[0] < CACHE_SECONDS:
        return cached[1].model_copy(update={"cached": True})

    provider_name = "deterministic"
    model = "deterministic-v1"
    fallback_used = False
    reason: str | None = None
    provider = DeterministicProvider()
    if settings.enabled and settings.provider == "openai" and settings.api_key:
        try:
            openai_provider = OpenAiProvider(
                settings.api_key,
                settings.model,
                settings.reasoning_effort,
                settings.timeout_seconds,
            )
            answer = await openai_provider.answer_research_question(context, question)
            provider = openai_provider
            provider_name = "openai"
            model = settings.model
        except Exception as exc:
            reason = fallback_reason(exc)
            fallback_used = True
            logger.warning("AI research failed ticker=%s provider=openai reason=%s", ticker, reason)
            answer = await provider.answer_research_question(context, question)
    else:
        fallback_used = settings.provider == "openai"
        reason = "disabled" if not settings.enabled else "api_key_missing" if settings.provider == "openai" else None
        answer = await provider.answer_research_question(context, question)

    result = AiResearchResponse(
        ticker=context.ticker,
        answer=answer,
        provider=provider_name,
        model=model,
        fallback_used=fallback_used,
        fallback_reason=reason,
        generated_at=datetime.now(UTC),
        cached=False,
        input_tokens=provider.input_tokens,
        output_tokens=provider.output_tokens,
    )
    _cache[cache_key] = (time.monotonic(), result)
    logger.info(
        "AI research ticker=%s provider=%s model=%s latency_ms=%.1f fallback=%s",
        context.ticker,
        provider_name,
        model,
        (time.perf_counter() - started) * 1000,
        fallback_used,
    )
    return result
