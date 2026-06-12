import json
import logging
import os
import threading
import uuid
from concurrent.futures import ThreadPoolExecutor
from dataclasses import asdict, dataclass
from datetime import UTC, datetime
from pathlib import Path
from typing import Callable, Literal

from app.model_training import train_model_for_ticker, training_lock
from app.model_registry import normalize_ticker

logger = logging.getLogger(__name__)
JobStatus = Literal["queued", "running", "succeeded", "failed"]


@dataclass(frozen=True)
class TrainingJob:
    job_id: str
    ticker: str
    period: str
    status: JobStatus
    submitted_at: str
    started_at: str | None = None
    completed_at: str | None = None
    error: str | None = None
    accuracy: float | None = None
    precision: float | None = None
    recall: float | None = None
    model_path: str | None = None
    trained_at: str | None = None


class TrainingJobManager:
    def __init__(
        self,
        project_root: Path,
        trainer: Callable = train_model_for_ticker,
        max_workers: int | None = None,
    ) -> None:
        configured_dir = os.getenv("MODEL_DIR", "").strip()
        models_dir = Path(configured_dir).resolve() if configured_dir else project_root.resolve() / "models"
        self.jobs_dir = models_dir / "jobs"
        self.trainer = trainer
        self._lock = threading.Lock()
        self._jobs: dict[str, TrainingJob] = {}
        workers = max_workers or max(1, int(os.getenv("TRAINING_WORKERS", "2")))
        self._executor = ThreadPoolExecutor(max_workers=workers, thread_name_prefix="model-training")
        self._load_jobs()

    def submit(self, ticker: str, period: str) -> TrainingJob:
        normalized_ticker = normalize_ticker(ticker)
        with self._lock:
            active = next(
                (
                    job for job in self._jobs.values()
                    if job.ticker == normalized_ticker and job.status in {"queued", "running"}
                ),
                None,
            )
            if active:
                return active
            job = TrainingJob(
                job_id=str(uuid.uuid4()),
                ticker=normalized_ticker,
                period=period,
                status="queued",
                submitted_at=datetime.now(UTC).isoformat(),
            )
            self._save_locked(job)
        self._executor.submit(self._run, job.job_id)
        return job

    def get(self, job_id: str) -> TrainingJob | None:
        with self._lock:
            return self._jobs.get(job_id)

    def list_for_ticker(self, ticker: str, limit: int = 10) -> list[TrainingJob]:
        normalized_ticker = normalize_ticker(ticker)
        with self._lock:
            jobs = [job for job in self._jobs.values() if job.ticker == normalized_ticker]
        return sorted(jobs, key=lambda job: job.submitted_at, reverse=True)[:limit]

    def shutdown(self) -> None:
        self._executor.shutdown(wait=True)

    def _run(self, job_id: str) -> None:
        job = self.get(job_id)
        if job is None:
            return
        self._update(job_id, status="running", started_at=datetime.now(UTC).isoformat())
        try:
            with training_lock(job.ticker):
                outcome = self.trainer(job.ticker, job.period)
            self._update(
                job_id,
                status="succeeded",
                completed_at=datetime.now(UTC).isoformat(),
                accuracy=outcome.metadata.accuracy,
                precision=outcome.metadata.precision,
                recall=outcome.metadata.recall,
                model_path=outcome.metadata.model_path,
                trained_at=outcome.metadata.trained_at,
            )
        except Exception as exc:
            logger.exception("Background model training failed job=%s ticker=%s", job_id, job.ticker)
            self._update(
                job_id,
                status="failed",
                completed_at=datetime.now(UTC).isoformat(),
                error=str(exc) or "Model training failed.",
            )

    def _update(self, job_id: str, **changes) -> TrainingJob:
        with self._lock:
            current = self._jobs[job_id]
            payload = asdict(current)
            payload.update(changes)
            updated = TrainingJob(**payload)
            self._save_locked(updated)
            return updated

    def _save_locked(self, job: TrainingJob) -> None:
        self.jobs_dir.mkdir(parents=True, exist_ok=True)
        path = self.jobs_dir / f"{job.job_id}.json"
        temporary_path = path.with_suffix(".json.tmp")
        temporary_path.write_text(json.dumps(asdict(job), indent=2) + "\n", encoding="utf-8")
        temporary_path.replace(path)
        self._jobs[job.job_id] = job

    def _load_jobs(self) -> None:
        if not self.jobs_dir.is_dir():
            return
        for path in self.jobs_dir.glob("*.json"):
            try:
                payload = json.loads(path.read_text(encoding="utf-8"))
                if payload.get("status") in {"queued", "running"}:
                    payload.update(
                        status="failed",
                        completed_at=datetime.now(UTC).isoformat(),
                        error="Training was interrupted by a service restart. Start a new job.",
                    )
                job = TrainingJob(**payload)
                with self._lock:
                    self._save_locked(job)
            except (OSError, TypeError, ValueError, json.JSONDecodeError):
                logger.warning("Ignoring invalid training job record path=%s", path)
