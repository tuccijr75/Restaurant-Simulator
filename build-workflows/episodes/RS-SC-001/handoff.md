# Episode Handoff — RS-SC-001

This episode handoff mirrors `build-workflows/handoffs/RS-SC-001-handoff.md`.

## Files Changed

- build-workflows/task-briefs/current-task.md
- build-workflows/state/state.json
- tests/test_engine_contract.py

## Files Created

- build-workflows/task-briefs/RS-SC-001.md
- build-workflows/audits/RS-SC-001-audit.md
- build-workflows/handoffs/RS-SC-001-handoff.md
- build-workflows/episodes/RS-SC-001/task.md
- build-workflows/episodes/RS-SC-001/trace.jsonl
- build-workflows/episodes/RS-SC-001/checks.jsonl
- build-workflows/episodes/RS-SC-001/audit.md
- build-workflows/episodes/RS-SC-001/handoff.md
- build-workflows/episodes/RS-SC-001/receipt.json

## Tests Added

- Schema/event enum parity.
- Deprecated `item.sold` absence.
- `item.taken -> item.completed` lifecycle.
- Ticket completion after item completion.

## Test Command

```bash
python -m unittest discover -s tests
```

## Next Task

RS-EN-001 — Engine lifecycle/provenance verification.
