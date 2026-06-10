# Episode Handoff — RS-EN-001

This episode handoff mirrors `build-workflows/handoffs/RS-EN-001-handoff.md`.

## Files Changed

- build-workflows/task-briefs/current-task.md
- build-workflows/state/state.json
- restaurant_simulator/engine.py
- tests/test_engine_contract.py

## Files Created

- build-workflows/task-briefs/RS-EN-001.md
- build-workflows/audits/RS-EN-001-audit.md
- build-workflows/handoffs/RS-EN-001-handoff.md
- build-workflows/episodes/RS-EN-001/task.md
- build-workflows/episodes/RS-EN-001/trace.jsonl
- build-workflows/episodes/RS-EN-001/checks.jsonl
- build-workflows/episodes/RS-EN-001/audit.md
- build-workflows/episodes/RS-EN-001/handoff.md
- build-workflows/episodes/RS-EN-001/receipt.json

## Engine Changes

- Added source-pack/task/workflow/data-classification provenance.
- Added `run_metadata`.
- Added provenance to ledgers, validation datasets, summary, scenario config, and run receipt.
- Added `item_instance_id` to `item.taken` and `item.completed`.

## Test Command

```bash
python -m unittest discover -s tests
```

## Next Task

RS-VA-001 — Determinism and output gate verification.
