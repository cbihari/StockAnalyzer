import os
import time
from collections import defaultdict, deque

from fastapi import APIRouter, HTTPException, Request, status

from app.ai.explanation_service import generate_explanation
from app.ai.research_service import answer_research_question
from app.ai.schemas import AiExplainRequest, AiExplainResponse, AiResearchRequest, AiResearchResponse

router = APIRouter(prefix="/ai", tags=["ai"])
_requests: dict[str, deque[float]] = defaultdict(deque)


def _check_rate_limit(request: Request) -> None:
    limit = max(1, int(os.getenv("AI_RATE_LIMIT_PER_MINUTE", "10")))
    key = request.headers.get("X-Forwarded-For", "").split(",", 1)[0].strip()
    key = key or (request.client.host if request.client else "unknown")
    now = time.monotonic()
    bucket = _requests[key]
    while bucket and now - bucket[0] >= 60:
        bucket.popleft()
    if len(bucket) >= limit:
        raise HTTPException(
            status_code=status.HTTP_429_TOO_MANY_REQUESTS,
            detail="AI explanation rate limit exceeded. Please try again shortly.",
        )
    bucket.append(now)


@router.post("/explain", response_model=AiExplainResponse)
async def explain(request_body: AiExplainRequest, request: Request) -> AiExplainResponse:
    _check_rate_limit(request)
    return await generate_explanation(request_body.ticker, request_body.force_refresh)


@router.post("/research", response_model=AiResearchResponse)
async def research(request_body: AiResearchRequest, request: Request) -> AiResearchResponse:
    _check_rate_limit(request)
    return await answer_research_question(request_body.ticker, request_body.question)
