from __future__ import annotations

import argparse
import json
from .core import SCENARIOS, run_to_path


def main() -> None:
    parser = argparse.ArgumentParser(description="Run Restaurant Daily Flow Simulator")
    parser.add_argument("--scenario-type", choices=SCENARIOS, default="normal_day")
    parser.add_argument("--seed", type=int, default=12345)
    parser.add_argument("--out", default="outputs/run")
    args = parser.parse_args()
    print(json.dumps(run_to_path(args.scenario_type, args.seed, args.out), indent=2, sort_keys=True))


if __name__ == "__main__":
    main()
