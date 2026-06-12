# Episode Task — RS-GC-001

## Task ID

RS-GC-001

## Task Name

Repository Tracking Hygiene Plan

## Runtime Class

T3

## Lane

Repository Hygiene

## Objective

Prepare a local-first tracking hygiene plan for generated Godot/editor/cache/build artifacts after `.gitignore` expansion.

## Safety Rule

Generated files must remain on the user's local disk. Tracking-status adjustment must be performed locally.

## Allowed Paths

- `build-workflows/state/state.json`
- `build-workflows/task-briefs/current-task.md`
- `build-workflows/task-briefs/RS-GC-001.md`
- `build-workflows/audits/RS-GC-001-audit.md`
- `build-workflows/handoffs/RS-GC-001-handoff.md`
- `build-workflows/episodes/RS-GC-001/**`

## Prohibited Paths

- `restaurant_simulator/**`
- `game/**`
- `tools/**`
- `docs/**`
- `outputs/**`
- `control-pack/active/**`
- `README.md`
- `pyproject.toml`
- AI Shift Commander repository or files

## Completion Criteria

- Tracking plan created.
- Workflow state updated.
- No runtime files changed.
- No generated files changed.
- Local action remains explicit and pending.
