import logging
import os
import time
import uuid
from datetime import UTC, datetime
from hashlib import sha256
from typing import Literal

from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse
from pydantic import BaseModel, Field, field_validator

from app.indicators import router as indicators_router
from app.ml_prediction import router as ml_prediction_router
from app.models import router as models_router
from app.rule_based_prediction import router as rule_based_prediction_router
from app.stock_history import router as stock_history_router

logging.basicConfig(
    level=os.getenv("LOG_LEVEL", "INFO").upper(),
    format="%(asctime)s %(levelname)s %(name)s %(message)s",
)

app = FastAPI(title="StockAnalyzer ML Service", version="0.1.0")
app.include_router(stock_history_router)
app.include_router(indicators_router)
app.include_router(rule_based_prediction_router)
app.include_router(ml_prediction_router)
app.include_router(models_router)
logger = logging.getLogger(__name__)


@app.middleware("http")
async def log_requests(request: Request, call_next):
    correlation_id = request.headers.get("X-Correlation-ID", uuid.uuid4().hex)
    started = time.perf_counter()
    try:
        response = await call_next(request)
    except Exception:
        logger.exception(
            "Unhandled request failure method=%s path=%s correlation_id=%s",
            request.method,
            request.url.path,
            correlation_id,
        )
        response = JSONResponse(
            status_code=500,
            content={
                "detail": "An unexpected error occurred.",
                "correlation_id": correlation_id,
            },
        )

    response.headers["X-Correlation-ID"] = correlation_id
    elapsed_ms = (time.perf_counter() - started) * 1000
    logger.info(
        "HTTP %s %s returned %s in %.1fms correlation_id=%s",
        request.method,
        request.url.path,
        response.status_code,
        elapsed_ms,
        correlation_id,
    )
    return response


class PredictionRequest(BaseModel):
    symbol: str = Field(min_length=1, max_length=10, pattern=r"^[A-Za-z.\-]+$")

    @field_validator("symbol")
    @classmethod
    def normalize_symbol(cls, value: str) -> str:
        return value.strip().upper()


class PredictionResponse(BaseModel):
    symbol: str
    direction: Literal["UP", "DOWN"]
    confidence: float
    model: str
    disclaimer: str


@app.get("/health")
def health() -> dict[str, str]:
    return {
        "status": "healthy",
        "service": "stock-analyzer-ml",
        "timestamp": datetime.now(UTC).isoformat(),
    }


@app.post("/predict", response_model=PredictionResponse)
def predict(request: PredictionRequest) -> PredictionResponse:
    # Deterministic placeholder only. Replace with trained inference later.
    score = int(sha256(request.symbol.encode()).hexdigest()[:8], 16)
    direction: Literal["UP", "DOWN"] = "UP" if score % 2 == 0 else "DOWN"
    confidence = round(0.51 + ((score % 1500) / 10000), 2)

    return PredictionResponse(
        symbol=request.symbol,
        direction=direction,
        confidence=confidence,
        model="deterministic-baseline-v0",
        disclaimer="Demo output only. Not financial advice or a trained ML prediction.",
    )
