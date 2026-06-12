# Episode Task — RS-AU-001

## Task ID

RS-AU-001

## Task Name

Post-change Repository Stabilization Audit

## Runtime Class

T3

## Lane

Audit / Stabilization

## Objective

Audit repository drift after major Godot/game/workflow changes, confirm ignore-rule protection for generated/editor/cache artifacts, and define the next safe cleanup step without deleting local files.

## Allowed Paths

- `.gitignore`
- `build-workflows/state/state.json`
- `build-workflows/task-briefs/current-task.md`
- `build-workflows/task-briefs/RS-AU-001.md`
- `build-workflows/audits/RS-AU-001-audit.md`
- `build-workflows/handoffs/RS-AU-001-handoff.md`
- `build-workflows/episodes/RS-AU-001/**`

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

- Change groups classified.
- Ignore-rule protection confirmed.
- No generated files deleted.
- Workflow-state conflict recorded.
- Next safe cleanup task named.
- Receipt created.
