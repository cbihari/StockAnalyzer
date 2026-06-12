import logging
import json
from pathlib import Path
from typing import Annotated, Literal

from fastapi import APIRouter, HTTPException, Query, status
from pydantic import BaseModel

from app.model_registry import ModelRegistry, normalize_ticker
from app.model_training import (
    InsufficientTrainingDataError,
    MissingOhlcvDataError,
    ModelTrainingFailedError,
    train_model_for_ticker,
    training_lock,
)
from app.training_jobs import TrainingJobManager

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/models", tags=["models"])
PROJECT_ROOT = Path(__file__).resolve().parents[1]
MODEL_REGISTRY = ModelRegistry(PROJECT_ROOT)
TRAINING_JOBS = TrainingJobManager(PROJECT_ROOT)


class ModelTrainingResponse(BaseModel):
    ticker: str
    status: Literal["trained"]
    accuracy: float
    precision: float
    recall: float
    model_path: str
    trained_at: str


class ModelMetricsResponse(BaseModel):
    ticker: str
    model_status: Literal["trained"]
    model_name: str
    trained_at: str
    accuracy: float
    precision: float
    recall: float
    confusion_matrix: list[list[int]]
    training_rows: int
    testing_rows: int


class ModelTrainingJobResponse(BaseModel):
    job_id: str
    ticker: str
    period: str
    status: Literal["queued", "running", "succeeded", "failed"]
    submitted_at: str
    started_at: str | None = None
    completed_at: str | None = None
    error: str | None = None
    accuracy: float | None = None
    precision: float | None = None
    recall: float | None = None
    model_path: str | None = None
    trained_at: str | None = None


class ModelVersionResponse(BaseModel):
    version_id: str
    ticker: str
    model_name: str
    model_path: str
    metrics_path: str
    trained_at: str
    training_rows: int
    test_rows: int
    accuracy: float
    precision: float
    recall: float
    features: list[str]
    confusion_matrix: list[list[int]]
    feature_importance: dict[str, float]
    is_active: bool


class ModelVersionsResponse(BaseModel):
    ticker: str
    versions: list[ModelVersionResponse]


@router.get(
    "/metrics",
    response_model=ModelMetricsResponse,
    summary="Get training metrics for a ticker-specific model",
)
def get_model_metrics(
    ticker: Annotated[
        str,
        Query(
            min_length=1,
            max_length=20,
            pattern=r"^[A-Za-z0-9.^=\-]+$",
            description="Yahoo Finance ticker symbol, for example TCS.NS",
        ),
    ],
) -> ModelMetricsResponse:
    normalized_ticker = normalize_ticker(ticker)
    metadata = MODEL_REGISTRY.load_model_metadata(normalized_ticker)
    if metadata is None or not MODEL_REGISTRY.model_exists(normalized_ticker):
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail="No trained model found for this ticker. Analyze or train this stock first.",
        )

    metrics_path = MODEL_REGISTRY.get_model_path(normalized_ticker).with_name(
        f"{MODEL_REGISTRY.get_model_path(normalized_ticker).stem}_metrics.json"
    )
    try:
        metrics = json.loads(metrics_path.read_text(encoding="utf-8"))
        confusion = metrics["confusion_matrix"]
    except (OSError, KeyError, TypeError, ValueError, json.JSONDecodeError) as exc:
        logger.exception("Could not read model metrics ticker=%s", normalized_ticker)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="The trained model metrics could not be loaded.",
        ) from exc

    return ModelMetricsResponse(
        ticker=metadata.ticker,
        model_status="trained",
        model_name=metadata.model_name,
        trained_at=metadata.trained_at,
        accuracy=metadata.accuracy,
        precision=metadata.precision,
        recall=metadata.recall,
        confusion_matrix=confusion,
        training_rows=metadata.training_rows,
        testing_rows=metadata.test_rows,
    )


@router.get(
    "/versions",
    response_model=ModelVersionsResponse,
    summary="List immutable model versions for a ticker",
)
def get_model_versions(
    ticker: Annotated[str, Query(min_length=1, max_length=20, pattern=r"^[A-Za-z0-9.^=\-]+$")],
) -> ModelVersionsResponse:
    normalized_ticker = normalize_ticker(ticker)
    return ModelVersionsResponse(
        ticker=normalized_ticker,
        versions=[
            ModelVersionResponse.model_validate(version, from_attributes=True)
            for version in MODEL_REGISTRY.list_model_versions(normalized_ticker)
        ],
    )


@router.post(
    "/train/jobs",
    response_model=ModelTrainingJobResponse,
    status_code=status.HTTP_202_ACCEPTED,
    summary="Queue ticker model training in the background",
)
def start_training_job(
    ticker: Annotated[str, Query(min_length=1, max_length=20, pattern=r"^[A-Za-z0-9.^=\-]+$")],
    period: Annotated[str, Query(description="Supported Yahoo Finance history period")] = "5y",
) -> ModelTrainingJobResponse:
    return ModelTrainingJobResponse.model_validate(
        TRAINING_JOBS.submit(ticker, period),
        from_attributes=True,
    )


@router.get(
    "/train/jobs/{job_id}",
    response_model=ModelTrainingJobResponse,
    summary="Get background model training status",
)
def get_training_job(job_id: str) -> ModelTrainingJobResponse:
    job = TRAINING_JOBS.get(job_id)
    if job is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Training job was not found.")
    return ModelTrainingJobResponse.model_validate(job, from_attributes=True)


@router.get(
    "/train/jobs",
    response_model=list[ModelTrainingJobResponse],
    summary="List recent training jobs for a ticker",
)
def get_training_jobs(
    ticker: Annotated[str, Query(min_length=1, max_length=20, pattern=r"^[A-Za-z0-9.^=\-]+$")],
) -> list[ModelTrainingJobResponse]:
    return [
        ModelTrainingJobResponse.model_validate(job, from_attributes=True)
        for job in TRAINING_JOBS.list_for_ticker(ticker)
    ]


@router.post(
    "/train",
    response_model=ModelTrainingResponse,
    summary="Train or retrain a ticker-specific Random Forest model",
)
def train_model(
    ticker: Annotated[
        str,
        Query(
            min_length=1,
            max_length=20,
            pattern=r"^[A-Za-z0-9.^=\-]+$",
            description="Yahoo Finance ticker symbol, for example TCS.NS",
        ),
    ],
    period: Annotated[
        str,
        Query(description="Supported Yahoo Finance history period, for example 5y"),
    ] = "5y",
) -> ModelTrainingResponse:
    normalized_ticker = normalize_ticker(ticker)
    try:
        with training_lock(normalized_ticker):
            outcome = train_model_for_ticker(normalized_ticker, period)
    except (InsufficientTrainingDataError, MissingOhlcvDataError) as exc:
        logger.warning("Model training rejected ticker=%s reason=%s", normalized_ticker, exc)
        raise HTTPException(
            status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
            detail=str(exc),
        ) from exc
    except ModelTrainingFailedError as exc:
        logger.exception("Model training failed ticker=%s", normalized_ticker)
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=str(exc),
        ) from exc

    return ModelTrainingResponse(
        ticker=outcome.metadata.ticker,
        status="trained",
        accuracy=outcome.metadata.accuracy,
        precision=outcome.metadata.precision,
        recall=outcome.metadata.recall,
        model_path=outcome.metadata.model_path,
        trained_at=outcome.metadata.trained_at,
    )
