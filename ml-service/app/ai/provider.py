from typing import Protocol

from app.ai.schemas import StockAiExplanation, StockAnalysisContext, StockResearchAnswer


class LlmProvider(Protocol):
    async def generate_stock_explanation(
        self,
        context: StockAnalysisContext,
    ) -> StockAiExplanation: ...

    async def answer_research_question(
        self,
        context: StockAnalysisContext,
        question: str,
    ) -> StockResearchAnswer: ...
