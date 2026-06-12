import json

from openai import AsyncOpenAI

from app.ai.prompts import (
    ASSISTANT_SYSTEM_INSTRUCTIONS,
    SYSTEM_INSTRUCTIONS,
    build_research_input,
    build_user_input,
)
from app.ai.schemas import StockAiExplanation, StockAnalysisContext, StockResearchAnswer


class OpenAiProvider:
    def __init__(
        self,
        api_key: str,
        model: str,
        reasoning_effort: str,
        timeout_seconds: float,
        client: AsyncOpenAI | None = None,
    ) -> None:
        self.model = model
        self.reasoning_effort = reasoning_effort
        self.client = client or AsyncOpenAI(api_key=api_key, timeout=timeout_seconds, max_retries=0)
        self.input_tokens: int | None = None
        self.output_tokens: int | None = None

    async def generate_stock_explanation(
        self,
        context: StockAnalysisContext,
    ) -> StockAiExplanation:
        response = await self.client.responses.parse(
            model=self.model,
            reasoning={"effort": self.reasoning_effort},
            instructions=SYSTEM_INSTRUCTIONS,
            input=build_user_input(json.dumps(context.model_dump(mode="json"), separators=(",", ":"))),
            text_format=StockAiExplanation,
            store=False,
        )
        usage = getattr(response, "usage", None)
        self.input_tokens = getattr(usage, "input_tokens", None)
        self.output_tokens = getattr(usage, "output_tokens", None)
        if response.output_parsed is None:
            raise ValueError("OpenAI returned no parsed explanation")
        if (
            response.output_parsed.ticker != context.ticker
            or response.output_parsed.prediction != context.prediction
            or response.output_parsed.confidence != context.confidence
        ):
            raise ValueError("OpenAI response did not preserve grounded prediction fields")
        return response.output_parsed

    async def answer_research_question(
        self,
        context: StockAnalysisContext,
        question: str,
    ) -> StockResearchAnswer:
        response = await self.client.responses.parse(
            model=self.model,
            reasoning={"effort": self.reasoning_effort},
            instructions=ASSISTANT_SYSTEM_INSTRUCTIONS,
            input=build_research_input(
                question,
                json.dumps(context.model_dump(mode="json"), separators=(",", ":")),
            ),
            text_format=StockResearchAnswer,
            store=False,
        )
        usage = getattr(response, "usage", None)
        self.input_tokens = getattr(usage, "input_tokens", None)
        self.output_tokens = getattr(usage, "output_tokens", None)
        result = response.output_parsed
        if result is None:
            raise ValueError("OpenAI returned no parsed research answer")
        if result.ticker != context.ticker or result.question != question:
            raise ValueError("OpenAI response did not preserve grounded request fields")
        return result
