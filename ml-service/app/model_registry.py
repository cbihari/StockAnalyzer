import json
import os
import re
import shutil
from dataclasses import asdict, dataclass
from datetime import UTC, datetime
from pathlib import Path


TICKER_PATTERN = re.compile(r"^[A-Z0-9.^=\-]{1,20}$")


def normalize_ticker(ticker: str) -> str:
    normalized = ticker.strip().upper()
    if not TICKER_PATTERN.fullmatch(normalized):
        raise ValueError(
            "Ticker must contain 1-20 letters, numbers, dots, carets, equals signs, or hyphens."
        )
    return normalized


def ticker_key(ticker: str) -> str:
    return "".join(
        character if character.isalnum() else "_"
        for character in normalize_ticker(ticker)
    )


@dataclass(frozen=True)
class ModelMetadata:
    ticker: str
    model_name: str
    model_path: str
    trained_at: str
    training_rows: int
    test_rows: int
    accuracy: float
    precision: float
    recall: float
    features: list[str]


@dataclass(frozen=True)
class ModelVersionMetadata:
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
    is_active: bool = False


class ModelRegistry:
    def __init__(self, project_root: Path) -> None:
        self.project_root = project_root.resolve()
        configured_dir = os.getenv("MODEL_DIR", "").strip()
        self.models_dir = Path(configured_dir).resolve() if configured_dir else self.project_root / "models"
        self.registry_dir = self.models_dir / "registry"
        self.versions_dir = self.models_dir / "versions"

    def get_model_path(self, ticker: str) -> Path:
        return self.models_dir / f"{ticker_key(ticker)}_random_forest.pkl"

    def get_metadata_path(self, ticker: str) -> Path:
        return self.registry_dir / f"{ticker_key(ticker)}_metadata.json"

    def get_version_dir(self, ticker: str, version_id: str) -> Path:
        return self.versions_dir / ticker_key(ticker) / version_id

    def create_version_id(self, trained_at: datetime | None = None) -> str:
        timestamp = (trained_at or datetime.now(UTC)).astimezone(UTC)
        return timestamp.strftime("%Y%m%dT%H%M%S%fZ")

    def model_exists(self, ticker: str) -> bool:
        return self.get_model_path(ticker).is_file()

    def save_model_metadata(
        self,
        *,
        ticker: str,
        model_name: str,
        training_rows: int,
        test_rows: int,
        accuracy: float,
        precision: float,
        recall: float,
        features: list[str],
        trained_at: datetime | None = None,
    ) -> ModelMetadata:
        normalized_ticker = normalize_ticker(ticker)
        model_path = self.get_model_path(normalized_ticker)
        try:
            relative_model_path = model_path.relative_to(self.project_root).as_posix()
        except ValueError:
            relative_model_path = model_path.as_posix()

        metadata = ModelMetadata(
            ticker=normalized_ticker,
            model_name=model_name,
            model_path=relative_model_path,
            trained_at=(trained_at or datetime.now(UTC)).astimezone(UTC).isoformat(),
            training_rows=int(training_rows),
            test_rows=int(test_rows),
            accuracy=float(accuracy),
            precision=float(precision),
            recall=float(recall),
            features=list(features),
        )

        metadata_path = self.get_metadata_path(normalized_ticker)
        metadata_path.parent.mkdir(parents=True, exist_ok=True)
        temporary_path = metadata_path.with_suffix(".json.tmp")
        temporary_path.write_text(
            json.dumps(asdict(metadata), indent=2) + "\n",
            encoding="utf-8",
        )
        temporary_path.replace(metadata_path)
        return metadata

    def load_model_metadata(self, ticker: str) -> ModelMetadata | None:
        metadata_path = self.get_metadata_path(ticker)
        if not metadata_path.is_file():
            return None

        payload = json.loads(metadata_path.read_text(encoding="utf-8"))
        return ModelMetadata(**payload)

    def save_model_version(
        self,
        *,
        ticker: str,
        model_name: str,
        source_model_path: Path,
        source_metrics_path: Path,
        training_rows: int,
        test_rows: int,
        accuracy: float,
        precision: float,
        recall: float,
        features: list[str],
        confusion_matrix: list[list[int]],
        feature_importance: dict[str, float],
        trained_at: datetime,
    ) -> ModelVersionMetadata:
        normalized_ticker = normalize_ticker(ticker)
        version_id = self.create_version_id(trained_at)
        version_dir = self.get_version_dir(normalized_ticker, version_id)
        temporary_dir = version_dir.with_name(f"{version_dir.name}.tmp")
        shutil.rmtree(temporary_dir, ignore_errors=True)
        temporary_dir.mkdir(parents=True, exist_ok=True)

        version_model_path = temporary_dir / source_model_path.name
        version_metrics_path = temporary_dir / source_metrics_path.name
        shutil.copy2(source_model_path, version_model_path)
        shutil.copy2(source_metrics_path, version_metrics_path)

        final_model_path = version_dir / source_model_path.name
        final_metrics_path = version_dir / source_metrics_path.name
        metadata = ModelVersionMetadata(
            version_id=version_id,
            ticker=normalized_ticker,
            model_name=model_name,
            model_path=self._relative_path(final_model_path),
            metrics_path=self._relative_path(final_metrics_path),
            trained_at=trained_at.astimezone(UTC).isoformat(),
            training_rows=int(training_rows),
            test_rows=int(test_rows),
            accuracy=float(accuracy),
            precision=float(precision),
            recall=float(recall),
            features=list(features),
            confusion_matrix=confusion_matrix,
            feature_importance=feature_importance,
        )
        (temporary_dir / "metadata.json").write_text(
            json.dumps(asdict(metadata), indent=2) + "\n",
            encoding="utf-8",
        )
        version_dir.parent.mkdir(parents=True, exist_ok=True)
        temporary_dir.replace(version_dir)
        return metadata

    def list_model_versions(self, ticker: str) -> list[ModelVersionMetadata]:
        normalized_ticker = normalize_ticker(ticker)
        active = self.load_model_metadata(normalized_ticker)
        ticker_versions_dir = self.versions_dir / ticker_key(normalized_ticker)
        if not ticker_versions_dir.is_dir():
            return []

        versions: list[ModelVersionMetadata] = []
        for metadata_path in ticker_versions_dir.glob("*/metadata.json"):
            try:
                payload = json.loads(metadata_path.read_text(encoding="utf-8"))
                payload["is_active"] = bool(
                    active and payload.get("trained_at") == active.trained_at
                )
                versions.append(ModelVersionMetadata(**payload))
            except (OSError, TypeError, ValueError, json.JSONDecodeError):
                continue
        return sorted(versions, key=lambda item: item.trained_at, reverse=True)

    def _relative_path(self, path: Path) -> str:
        try:
            return path.relative_to(self.project_root).as_posix()
        except ValueError:
            return path.as_posix()
