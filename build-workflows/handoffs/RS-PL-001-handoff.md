# Handoff — RS-PL-001

## Project

Restaurant Daily Flow Simulator

## Task

RS-PL-001 — Plausibility and Realism Validation Profile

## Status

Ready for review. Local test run required.

## Changes

Added:

- `profiles/plausibility.json`
- `tests/test_plausibility.py`
- RS-PL-001 workflow records

Updated:

- `build-workflows/state/state.json`
- `build-workflows/task-briefs/current-task.md`

Simulator runtime files were not changed.

## Validation Intent

The profile and tests check whether generated simulator outputs remain synthetic, deterministic, causally shaped, and operationally plausible across the required scenario set.

## Local Command Required

```powershell
python -m unittest discover -s tests
```

## Completion Rule

If tests pass, record RS-PL-001 as completed. If tests report issues, create a follow-up task for the affected layer.

## Recommended Next Task After Completion

RS-CX-001 — AI Shift Commander compatibility profile.
