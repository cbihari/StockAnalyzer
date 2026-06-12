import time
from datetime import UTC, datetime

from sklearn.ensemble import RandomForestClassifier

from app.model_registry import ModelMetadata
from app.model_training import ModelTrainingOutcome
from app.training import TrainingResult
from app.training_jobs import TrainingJobManager


def successful_outcome(ticker: str) -> ModelTrainingOutcome:
    result = TrainingResult(
        train_rows=800,
        test_rows=200,
        train_start="2021-01-01",
        train_end="2025-01-01",
        test_start="2025-01-02",
        test_end="2026-01-01",
        accuracy=0.6,
        precision=0.61,
        recall=0.59,
        confusion_matrix=[[70, 30], [50, 50]],
        feature_importance={"close": 1.0},
    )
    metadata = ModelMetadata(
        ticker=ticker,
        model_name="RandomForestClassifier",
        model_path=f"models/{ticker}_random_forest.pkl",
        trained_at=datetime.now(UTC).isoformat(),
        training_rows=800,
        test_rows=200,
        accuracy=0.6,
        precision=0.61,
        recall=0.59,
        features=["close"],
    )
    return ModelTrainingOutcome(RandomForestClassifier(), result, metadata)


def wait_for_completion(manager: TrainingJobManager, job_id: str):
    deadline = time.monotonic() + 2
    while time.monotonic() < deadline:
        job = manager.get(job_id)
        if job and job.status in {"succeeded", "failed"}:
            return job
        time.sleep(0.01)
    raise AssertionError("Training job did not complete")


def test_background_job_persists_successful_metrics(tmp_path) -> None:
    manager = TrainingJobManager(
        tmp_path,
        trainer=lambda ticker, period: successful_outcome(ticker),
        max_workers=1,
    )

    submitted = manager.submit("aapl", "5y")
    completed = wait_for_completion(manager, submitted.job_id)
    manager.shutdown()

    assert completed.status == "succeeded"
    assert completed.ticker == "AAPL"
    assert completed.accuracy == 0.6
    assert (tmp_path / "models" / "jobs" / f"{submitted.job_id}.json").is_file()


def test_duplicate_active_job_is_reused(tmp_path) -> None:
    def slow_trainer(ticker: str, period: str):
        time.sleep(0.05)
        return successful_outcome(ticker)

    manager = TrainingJobManager(tmp_path, trainer=slow_trainer, max_workers=1)

    first = manager.submit("MSFT", "5y")
    second = manager.submit("msft", "5y")
    wait_for_completion(manager, first.job_id)
    manager.shutdown()

    assert second.job_id == first.job_id
