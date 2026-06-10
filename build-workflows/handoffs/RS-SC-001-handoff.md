# Handoff — RS-SC-001

## Project

Restaurant Daily Flow Simulator

## Task

RS-SC-001 — Schema and Contract Regression Alignment

## Objective

Add source-pack-aware contract regressions for the split item lifecycle and deprecated `item.sold` removal.

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

## Source Packets Used

- 00 Security
- 01 System
- 02 Workflow
- 03 Schema
- 04 Context
- 05 Diagnostics

## Tests Added

- Schema event enum parity test against `EVENT_TYPES`.
- Deprecated `item.sold` absence test.
- `item.taken -> item.completed` lifecycle test.
- Ticket completion after all item completions test.

## Tests Run

Not run in connector environment.

Run locally:

```bash
python -m unittest discover -s tests
```

## Source Conflicts Found

None blocking.

## Assumptions Introduced

- Existing engine lifecycle is correct enough for contract-level regression testing.
- Local/CI execution will provide final runtime confirmation.

## Blockers

- Local/CI test run pending.

## Next Recommended Task

RS-EN-001 — Engine lifecycle/provenance verification.

Purpose: run or locally verify the engine output, ensure receipts/provenance align with the active workflow source pack, and address any runtime failure caused by the new contract regressions.

## Approval Required Status

No additional approval required for review. Runtime code changes require an implementation task scope.
