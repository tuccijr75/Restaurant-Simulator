# Restaurant Daily Flow Simulator

Deterministic synthetic fast-food restaurant operations simulator for AI Shift Commander development, validation, training, and stress testing.

## V1 scope

- Standalone simulator, no live ASC dependency.
- Synthetic data only.
- No external APIs.
- No real customer or employee identifiers.
- No employee scoring, ranking, disciplinary analytics, or individual performance profiling.
- No brand-specific assumptions.
- No random-only behavior; bounded variation is seeded inside causal rules.

## Outputs

- `event_stream.jsonl`
- `inventory_ledger.json`
- `staffing_ledger.json`
- `recommendation_validation_dataset.json`
- `alert_validation_dataset.json`
- `end_of_shift_summary.json`
- `run_receipt.json`
- `hashes.json`

## Run

```bash
python -m restaurant_simulator.cli --scenario-type normal_day --seed 12345 --out outputs/normal_12345
```

## Test

```bash
python -m unittest discover -s tests
```

## Scenario types

`normal_day`, `slow_day`, `rush_day`, `weather_disruption`, `staffing_call_off`, `equipment_failure`, `local_event_surge`, `school_event_surge`, `holiday_pattern`, `multi_rush_condition`.

ASC compatibility is marked `pending_contract` until the final ASC ingestion contract is reconciled.
