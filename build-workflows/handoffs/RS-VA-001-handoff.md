# Handoff — RS-VA-001

## Project

Restaurant Daily Flow Simulator

## Task

RS-VA-001 — Determinism and Output Gate Verification

## Objective

Add and document validation-gate tests for deterministic replay and output bundle verification without changing runtime code.

## Files Changed

- build-workflows/task-briefs/current-task.md
- build-workflows/state/state.json

## Files Created

- build-workflows/task-briefs/RS-VA-001.md
- tests/test_validation_gate.py
- build-workflows/audits/RS-VA-001-audit.md
- build-workflows/handoffs/RS-VA-001-handoff.md
- build-workflows/episodes/RS-VA-001/task.md
- build-workflows/episodes/RS-VA-001/trace.jsonl
- build-workflows/episodes/RS-VA-001/checks.jsonl
- build-workflows/episodes/RS-VA-001/audit.md
- build-workflows/episodes/RS-VA-001/handoff.md
- build-workflows/episodes/RS-VA-001/receipt.json

## Runtime Changes

None.

## Tests Added

- Deterministic replay across all scenarios.
- Internal validation status across all scenarios.
- Deprecated event exclusion across all scenarios.
- Synthetic/governance-safe output scan.
- Output bundle completeness.
- Output digest verification.
- JSON/JSONL parseability.

## Tests Run

Not run in connector environment.

Run locally:

```bash
python -m unittest discover -s tests
```

## Blockers

Local/CI test execution pending.

## Next Recommended Task

RS-PL-001 — Plausibility and realism validation profile.

## Approval Required Status

No additional approval required for review. If tests fail, corrective work should be scoped into the appropriate engine or validation follow-up task.
