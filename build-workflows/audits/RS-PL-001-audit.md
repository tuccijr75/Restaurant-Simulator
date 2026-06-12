# Audit — RS-PL-001

## Task

RS-PL-001 — Plausibility and Realism Validation Profile

## Scope

Added a reusable plausibility profile and Python regression tests for synthetic restaurant operations realism checks.

## Files Created

- `profiles/plausibility.json`
- `tests/test_plausibility.py`
- `build-workflows/audits/RS-PL-001-audit.md`
- `build-workflows/handoffs/RS-PL-001-handoff.md`
- `build-workflows/episodes/RS-PL-001/task.md`
- `build-workflows/episodes/RS-PL-001/trace.jsonl`
- `build-workflows/episodes/RS-PL-001/checks.jsonl`
- `build-workflows/episodes/RS-PL-001/audit.md`
- `build-workflows/episodes/RS-PL-001/handoff.md`
- `build-workflows/episodes/RS-PL-001/receipt.json`

## Files Modified

- `build-workflows/state/state.json`
- `build-workflows/task-briefs/current-task.md`

## Runtime Files Modified

None.

## Coverage Added

- Required scenarios
- Required dayparts
- Required channels
- Required event types
- Deprecated `item.sold` exclusion
- Daypart demand shape
- Channel and menu mix coverage
- Scenario directional effects
- Weather shift evidence
- Staffing call-off evidence
- Equipment constraint evidence
- Local event, school event, and holiday evidence
- Multi-rush overload and recovery evidence
- Item lifecycle coherence
- Inventory and staffing ledger reconciliation
- Recommendation and alert datasets
- Synthetic-data and non-punitive output checks

## Fresh Bundle Review Inputs

The latest user-confirmed Godot run produced a valid hash match for `event_stream.jsonl`. The uploaded bundle summary reported 522 orders, 521 completed tickets, 17.61% labor, 87.02 average customer satisfaction, no abandoned tickets, and passing summary gates.

## Watch Items

- Drive-thru and lobby service times are very fast in the Godot sample and should be reviewed against future operator calibration.
- Equipment condition can degrade sharply in the Godot sample while no maintenance spend appears; this should be reviewed during future equipment realism passes.
- `F-14`, `F-15`, and `F-16` remain open decisions from RS-3D-001.

## Required Local Verification

Run:

```powershell
python -m unittest discover -s tests
```

## Status

Ready for review. Local test run required before marking completed.
