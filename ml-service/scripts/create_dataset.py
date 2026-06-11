#!/usr/bin/env python3
import argparse
import logging
import sys
from pathlib import Path

PROJECT_ROOT = Path(__file__).resolve().parents[1]
if str(PROJECT_ROOT) not in sys.path:
    sys.path.insert(0, str(PROJECT_ROOT))

from app.dataset import prepare_dataset, save_dataset
from app.stock_history import fetch_stock_history

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s %(levelname)s %(name)s %(message)s",
)
logger = logging.getLogger(__name__)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Create a leakage-safe stock direction training dataset."
    )
    parser.add_argument("--ticker", required=True, help="Yahoo Finance ticker, e.g. RELIANCE.NS")
    parser.add_argument("--period", default="5y", help="History period, e.g. 1y, 5y, max")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    try:
        ticker, period, history = fetch_stock_history(args.ticker, args.period)
        dataset = prepare_dataset(history)
        output_path = save_dataset(dataset, ticker, PROJECT_ROOT / "data" / "processed")
    except Exception:
        logger.exception("Dataset creation failed ticker=%s period=%s", args.ticker, args.period)
        return 1

    logger.info(
        "Saved %d processed rows for %s (%s) to %s",
        len(dataset),
        ticker,
        period,
        output_path,
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
