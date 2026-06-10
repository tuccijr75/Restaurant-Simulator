# Audit — RS-EN-001

## Task

RS-EN-001 — Engine Lifecycle and Provenance Verification

## Scope

Verified and corrected Python engine lifecycle/provenance behavior against the active Restaurant Simulator source-pack rules.

## Source Packets Used

- control-pack/manifest.json
- control-pack/active/00_security/00_security.md
- control-pack/active/01_system/01_system.md
- control-pack/active/02_workflow/02_workflow.md
- control-pack/active/03_schema/03_schema.json
- control-pack/active/04_context/04_context.md
- control-pack/active/05_diagnostics/05_diagnostics.md
- build-workflows/episodes/RS-SC-001/receipt.json

## Repository Paths Inspected

- build-workflows/task-briefs/current-task.md
- build-workflows/state/state.json
- build-workflows/episodes/RS-SC-001/receipt.json
- control-pack/active/04_context/04_context.md
- restaurant_simulator/engine.py
- restaurant_simulator/core.py
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

## Files Modified

- build-workflows/task-briefs/current-task.md
- build-workflows/state/state.json
- restaurant_simulator/engine.py
- tests/test_engine_contract.py

## Engine Findings

- Event lifecycle already used `item.taken` and `item.completed`.
- Runtime did not emit deprecated `item.sold`.
- Ticket completion followed item completion in current engine flow.
- Output provenance was incomplete for source-pack requirements.

## Engine Changes

- Added `SOURCE_PACK_VERSION`, `DEFAULT_TASK_ID`, `DEFAULT_WORKFLOW_ID`, and `DATA_CLASSIFICATION` constants.
- Added centralized provenance generation.
- Added provenance to scenario config, run metadata, inventory ledger, staffing ledger, recommendation validation dataset, alert validation dataset, end-of-shift summary, and run receipt.
- Added `item_instance_id` to `item.taken` and matching `item.completed` payloads for explicit lifecycle traceability.
- Bumped generator version to `sim-0.3.1`.

## Test Changes

- Added provenance field regression coverage.
- Strengthened item lifecycle tests to match `item_instance_id` from taken to completed.
- Added run receipt provenance checks.

## Tests Run

Not run in connector environment. Required local/CI command:

```bash
python -m unittest discover -s tests
```

## Security Review

Passed. No real data, secrets, live connectors, employee identifiers, employee scoring, ASC writes, deployment changes, or public claims introduced.

## Path Scope Result

Passed. Only allowed files were modified or created.

## Source Conflicts

None blocking.

## Blockers

Local/CI test execution pending.

## Result

Ready for review with pending local/CI test execution.
