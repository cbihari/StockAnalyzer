import json
import re
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


class ModelRegistry:
    def __init__(self, project_root: Path) -> None:
        self.project_root = project_root.resolve()
        self.models_dir = self.project_root / "models"
        self.registry_dir = self.models_dir / "registry"

    def get_model_path(self, ticker: str) -> Path:
        return self.models_dir / f"{ticker_key(ticker)}_random_forest.pkl"

    def get_metadata_path(self, ticker: str) -> Path:
        return self.registry_dir / f"{ticker_key(ticker)}_metadata.json"

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
