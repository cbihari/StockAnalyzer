import hashlib
import json
import logging
import os
import time
from dataclasses import dataclass
from datetime import UTC, datetime

from app.ai.context_builder import build_stock_analysis_context
from app.ai.deterministic_provider import DeterministicProvider
from app.ai.openai_provider import OpenAiProvider
from app.ai.prompts import PROMPT_VERSION
from app.ai.schemas import AiExplainResponse, StockAnalysisContext

logger = logging.getLogger(__name__)
CACHE_SECONDS = 900
_cache: dict[str, tuple[float, AiExplainResponse]] = {}


@dataclass(frozen=True)
class ProviderSettings:
    enabled: bool
    provider: str
    api_key: str
    model: str
    reasoning_effort: str
    timeout_seconds: float

    @classmethod
    def from_environment(cls) -> "ProviderSettings":
        api_key = os.getenv("OPENAI_API_KEY", "").strip()
        api_key_file = os.getenv("OPENAI_API_KEY_FILE", "").strip()
        if not api_key and api_key_file:
            try:
                with open(api_key_file, encoding="utf-8") as secret_file:
                    api_key = secret_file.read().strip()
            except OSError:
                logger.warning("OpenAI API key file could not be read")
        return cls(
            enabled=os.getenv("AI_EXPLANATIONS_ENABLED", "true").lower() == "true",
            provider=os.getenv("LLM_PROVIDER", "openai").lower(),
            api_key=api_key,
            model=os.getenv("OPENAI_MODEL", "gpt-5.5"),
            reasoning_effort=os.getenv("OPENAI_REASONING_EFFORT", "low"),
            timeout_seconds=max(1, float(os.getenv("OPENAI_TIMEOUT_SECONDS", "30"))),
        )


def context_hash(context: StockAnalysisContext, model: str) -> str:
    payload = {
        "context": context.model_dump(mode="json"),
        "model": model,
        "prompt_version": PROMPT_VERSION,
    }
    return hashlib.sha256(json.dumps(payload, sort_keys=True, separators=(",", ":")).encode()).hexdigest()


def fallback_reason(exc: Exception) -> str:
    name = type(exc).__name__.lower()
    if "timeout" in name:
        return "timeout"
    if "ratelimit" in name or "rate_limit" in name:
        return "rate_limited"
    if "refusal" in name:
        return "refused"
    return "provider_unavailable"


async def generate_explanation(ticker: str, force_refresh: bool) -> AiExplainResponse:
    started = time.perf_counter()
    settings = ProviderSettings.from_environment()
    context = await build_stock_analysis_context(ticker)
    requested_model = settings.model if settings.provider == "openai" else "deterministic-v1"
    input_hash = context_hash(context, requested_model)
    if not force_refresh:
        cached = _cache.get(input_hash)
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
            explanation = await openai_provider.generate_stock_explanation(context)
            provider = openai_provider
            provider_name = "openai"
            model = settings.model
        except Exception as exc:
            reason = fallback_reason(exc)
            fallback_used = True
            logger.warning("AI provider failed ticker=%s provider=openai reason=%s", ticker, reason)
            explanation = await provider.generate_stock_explanation(context)
    else:
        fallback_used = settings.provider == "openai"
        reason = "disabled" if not settings.enabled else "api_key_missing" if settings.provider == "openai" else None
        explanation = await provider.generate_stock_explanation(context)

    result = AiExplainResponse(
        ticker=context.ticker,
        explanation=explanation,
        provider=provider_name,
        model=model,
        fallback_used=fallback_used,
        fallback_reason=reason,
        generated_at=datetime.now(UTC),
        cached=False,
        input_hash=input_hash,
        prediction_type=context.prediction_type,
        prompt_version=PROMPT_VERSION,
        input_tokens=provider.input_tokens,
        output_tokens=provider.output_tokens,
    )
    _cache[input_hash] = (time.monotonic(), result)
    logger.info(
        "AI explanation ticker=%s provider=%s model=%s latency_ms=%.1f fallback=%s",
        context.ticker,
        provider_name,
        model,
        (time.perf_counter() - started) * 1000,
        fallback_used,
    )
    return result
