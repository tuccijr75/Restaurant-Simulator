# Episode Task — RS-PL-001

## Task ID

RS-PL-001

## Task Name

Plausibility and Realism Validation Profile

## Runtime Class

T3

## Lane

Validation Profile

## Objective

Add a reusable plausibility profile and automated test suite for synthetic restaurant operations realism checks.

## Allowed Paths

- `build-workflows/state/state.json`
- `build-workflows/task-briefs/current-task.md`
- `build-workflows/task-briefs/RS-PL-001.md`
- `profiles/plausibility.json`
- `tests/test_plausibility.py`
- `build-workflows/audits/RS-PL-001-audit.md`
- `build-workflows/handoffs/RS-PL-001-handoff.md`
- `build-workflows/episodes/RS-PL-001/**`

## Prohibited Paths

- `restaurant_simulator/engine.py`
- `restaurant_simulator/core.py`
- `game/**`
- `outputs/**`
- `README.md`
- `pyproject.toml`
- `.github/**`
- `control-pack/active/**`
- AI Shift Commander repository or files

## Completion Criteria

- Plausibility profile exists.
- Plausibility tests exist.
- Runtime behavior remains unchanged.
- Local tests are run before completion.
