# Audit — RS-SC-001

## Task

RS-SC-001 — Schema and Contract Regression Alignment

## Scope

Aligned Python contract tests with the active source-pack workflow and split item lifecycle contract.

## Source Packets Used

- control-pack/manifest.json
- control-pack/active/00_security/00_security.md
- control-pack/active/01_system/01_system.md
- control-pack/active/02_workflow/02_workflow.md
- control-pack/active/03_schema/03_schema.json
- control-pack/active/04_context/04_context.md
- control-pack/active/05_diagnostics/05_diagnostics.md

## Repository Paths Inspected

- build-workflows/task-briefs/current-task.md
- build-workflows/state/state.json
- control-pack/active/01_system/01_system.md
- control-pack/active/03_schema/03_schema.json
- restaurant_simulator/engine.py
- restaurant_simulator/core.py
- game/scripts/sim/SimRunState.cs
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

## Files Modified

- build-workflows/task-briefs/current-task.md
- build-workflows/state/state.json
- tests/test_engine_contract.py

## Files Deleted

None.

## Path Scope Result

Passed. Runtime implementation files were inspected but not modified. Control-pack doctrine files were inspected but not modified.

## Contract Findings

- Active source pack declares `item.taken` and `item.completed`; `item.sold` is deprecated.
- Python engine `EVENT_TYPES` already matches the split lifecycle.
- Python engine emits `item.taken` before inventory consumption ledger entries and emits `item.completed` before terminal ticket completion.
- Godot simulation state also emits `item.taken` and `item.completed` through task lifecycle.

## Test Changes

Updated `tests/test_engine_contract.py` to add:

- schema event enum parity check against exported `EVENT_TYPES`
- regression rejecting deprecated `item.sold`
- lifecycle invariant check for `item.taken -> item.completed`
- terminal ticket completion check after all known items complete

## Tests Run

Not run in connector environment. Required command for local/CI verification:

```bash
python -m unittest discover -s tests
```

## Security Review

Passed. No real data, secrets, employee identifiers, employee scoring, ASC runtime connection, or external connectors were introduced.

## Source Conflicts

None blocking.

## Assumptions

- GitHub connector is allowed for this requested repo workflow task.
- Local tests should be run by the next local/CI validation step because connector cannot execute repo test suite.

## Blockers

- Local/CI test execution is pending.

## Result

Ready for review with pending local/CI test execution.
