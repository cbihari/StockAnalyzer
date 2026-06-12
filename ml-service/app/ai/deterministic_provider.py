from app.ai.schemas import (
    DISCLAIMER,
    ExplanationSignal,
    ResearchCitation,
    StockAiExplanation,
    StockAnalysisContext,
    StockResearchAnswer,
)


class DeterministicProvider:
    model = "deterministic-v1"
    input_tokens: int | None = None
    output_tokens: int | None = None

    async def generate_stock_explanation(
        self,
        context: StockAnalysisContext,
    ) -> StockAiExplanation:
        supporting: list[ExplanationSignal] = []
        conflicting: list[ExplanationSignal] = []

        def add(signal: str, explanation: str, supports_up: bool, importance: str) -> None:
            item = ExplanationSignal(
                signal=signal,
                explanation=explanation,
                importance=importance,
            )
            predicted_up = context.prediction == "UP"
            (supporting if supports_up == predicted_up else conflicting).append(item)

        if context.ema_20 is not None and context.ema_50 is not None:
            add(
                "EMA trend",
                f"EMA20 ({context.ema_20:.2f}) is {'above' if context.ema_20 >= context.ema_50 else 'below'} EMA50 ({context.ema_50:.2f}).",
                context.ema_20 >= context.ema_50,
                "high",
            )
        if context.macd is not None and context.macd_signal is not None:
            add(
                "MACD momentum",
                f"MACD ({context.macd:.2f}) is {'above' if context.macd >= context.macd_signal else 'below'} its signal ({context.macd_signal:.2f}).",
                context.macd >= context.macd_signal,
                "medium",
            )
        if context.rsi_14 is not None:
            if context.rsi_14 >= 70:
                add("RSI", f"RSI is {context.rsi_14:.1f}, an elevated reading.", False, "medium")
            elif context.rsi_14 <= 30:
                add("RSI", f"RSI is {context.rsi_14:.1f}, a depressed reading.", True, "medium")
        if context.news_sentiment != "UNAVAILABLE":
            add(
                "News headline tone",
                f"Selected recent headlines have {context.news_sentiment.lower()} deterministic sentiment.",
                context.news_sentiment == "POSITIVE",
                "low",
            )

        risks = list(context.data_limitations)
        if context.model_accuracy is None:
            risks.append("No historical holdout accuracy is available for this prediction.")
        if context.confidence < 60:
            risks.append("Model confidence is relatively low.")
        if conflicting:
            risks.append("Technical signals do not all point in the same direction.")
        risk_level = "HIGH" if len(risks) >= 3 else "MEDIUM" if risks or conflicting else "LOW"
        direction = "higher" if context.prediction == "UP" else "lower"
        return StockAiExplanation(
            ticker=context.ticker,
            prediction=context.prediction,
            confidence=context.confidence,
            summary=(
                f"StockAnalyzer estimates a {context.prediction} next-session direction with "
                f"{context.confidence}% model confidence. The available signals are "
                f"{'mixed' if conflicting else 'mostly aligned'}, so this is an uncertain research estimate."
            ),
            supporting_signals=supporting,
            conflicting_signals=conflicting,
            risk_level=risk_level,
            risk_factors=risks,
            what_could_change_the_view=[
                f"Price moving beyond the recent {'resistance' if context.prediction == 'DOWN' else 'support'} area.",
                "A reversal in EMA or MACD alignment.",
                "Material new information not present in the supplied headlines.",
            ],
            beginner_explanation=(
                f"The model currently leans toward a {direction} next close, but the "
                f"{context.confidence}% figure measures model confidence rather than certainty."
            ),
            data_limitations=context.data_limitations,
            disclaimer=DISCLAIMER,
        )

    async def answer_research_question(
        self,
        context: StockAnalysisContext,
        question: str,
    ) -> StockResearchAnswer:
        lowered = question.lower()
        citations: list[ResearchCitation] = []
        points: list[str] = []

        def cite(source: str, label: str, evidence: str) -> None:
            citations.append(ResearchCitation(
                source=source,
                label=label,
                evidence=evidence,
                observed_at=context.timestamp,
            ))

        asks_for_advice = any(term in lowered for term in ("should i", "buy", "sell", "hold", "invest"))
        if asks_for_advice:
            answer = (
                "I cannot decide whether you should buy, sell, or hold this stock. "
                f"StockAnalyzer currently estimates {context.prediction} with {context.confidence}% "
                "model confidence; use the evidence below as research context rather than a recommendation."
            )
        elif "rsi" in lowered:
            value = "unavailable" if context.rsi_14 is None else f"{context.rsi_14:.1f}"
            answer = f"The latest RSI(14) for {context.ticker} is {value}. RSI describes recent momentum, not future certainty."
            points.append("Read RSI together with trend and volume rather than in isolation.")
        elif "news" in lowered or "sentiment" in lowered:
            answer = (
                f"Recent selected headlines are classified as {context.news_sentiment.lower()}. "
                "This is a limited headline-tone signal and may not capture the full market reaction."
            )
            points.append(f"Headline coverage used: {len(context.news_titles)} article title(s).")
        elif "risk" in lowered or "uncertain" in lowered:
            answer = (
                f"The main uncertainty is that a {context.confidence}% confidence score is not certainty. "
                "Technical signals, new information, and price movement beyond the recent range can change the view."
            )
            points.extend(context.data_limitations or ["Short-horizon price direction remains uncertain."])
        elif "support" in lowered or "resistance" in lowered or "range" in lowered:
            answer = (
                f"The recent observed range places support near {context.support:.2f} and resistance near "
                f"{context.resistance:.2f}. These are historical reference areas, not guaranteed boundaries."
                if context.support is not None and context.resistance is not None
                else "Recent support and resistance values are unavailable in the supplied analysis."
            )
        elif any(term in lowered for term in ("prediction", "direction", "why", "signal", "conflict")):
            trend_agrees = (context.prediction == "UP" and context.trend == "BULLISH") or (
                context.prediction == "DOWN" and context.trend == "BEARISH"
            )
            answer = (
                f"The saved model currently leans {context.prediction} with {context.confidence}% confidence. "
                f"The technical trend is {context.trend.lower()}, so the visible evidence "
                f"{'broadly agrees with' if trend_agrees else 'conflicts with'} the model direction."
            )
            if context.ema_20 is not None and context.ema_50 is not None:
                points.append(
                    f"EMA20 is {'above' if context.ema_20 >= context.ema_50 else 'below'} EMA50."
                )
            if context.macd is not None and context.macd_signal is not None:
                points.append(
                    f"MACD is {'above' if context.macd >= context.macd_signal else 'below'} its signal line."
                )
            if not trend_agrees:
                points.append("The model direction and current trend do not align, which increases uncertainty.")
        elif "model" in lowered or "accuracy" in lowered or "confidence" in lowered:
            accuracy = "unavailable" if context.model_accuracy is None else f"{context.model_accuracy * 100:.1f}%"
            answer = (
                f"The {context.model} model reports {context.confidence}% confidence for {context.prediction}. "
                f"Its recorded holdout accuracy is {accuracy}; these measures describe different things."
            )
            points.append("Confidence describes this output; accuracy summarizes past test predictions.")
        else:
            answer = (
                f"StockAnalyzer currently leans {context.prediction} for {context.ticker} with "
                f"{context.confidence}% model confidence. The trend is {context.trend.lower()}, while "
                f"recent headline sentiment is {context.news_sentiment.lower()}."
            )
            points.append("The estimate combines model output with technical and market context.")

        cite(
            "prediction",
            "Latest prediction",
            f"{context.prediction} at {context.confidence}% confidence; up {context.probability_up:.2f}, down {context.probability_down:.2f}.",
        )
        cite(
            "technical_indicators",
            "Technical snapshot",
            f"Trend {context.trend}; RSI {context.rsi_14}; EMA20 {context.ema_20}; EMA50 {context.ema_50}.",
        )
        if "news" in lowered or "sentiment" in lowered:
            cite(
                "news_sentiment",
                "Headline sentiment",
                f"{context.news_sentiment} from {len(context.news_titles)} selected headline(s).",
            )
        if "model" in lowered or "accuracy" in lowered or "confidence" in lowered:
            cite(
                "model_performance",
                "Model record",
                f"{context.model}; status {context.model_status}; accuracy {context.model_accuracy}.",
            )
        if "support" in lowered or "resistance" in lowered or "range" in lowered:
            cite(
                "market_context",
                "Recent price range",
                f"Support {context.support}; resistance {context.resistance}.",
            )

        return StockResearchAnswer(
            ticker=context.ticker,
            question=question,
            answer=answer,
            key_points=points[:4],
            citations=citations[:4],
            limitations=context.data_limitations[:4],
            follow_up_questions=[
                "Which signals support the prediction?",
                "What are the main risks in this analysis?",
                "How should I interpret confidence versus model accuracy?",
            ],
            disclaimer=DISCLAIMER,
        )
