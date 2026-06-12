# Audit — RS-GC-001

## Task

RS-GC-001 — Repository Tracking Hygiene Plan

## Scope

Prepared a local-first plan for adjusting repository tracking status after `.gitignore` expansion. No runtime files, game files, docs, tools, control-pack files, or generated files were modified by this task.

## Why This Must Be Local-First

A GitHub API file operation cannot guarantee that generated files remain on the user's local disk. The safe path is to perform the tracking-status adjustment from the local clone after `.gitignore` is committed.

## Observed Artifact Groups To Adjust Locally

- `game/.godot/editor/**`
- `game/.godot/mono/temp/**`
- `game/.godot/shader_cache/**`
- `game/.godot/uid_cache.bin`
- compiled artifacts under `game/.godot/mono/temp/**`
- cache artifacts under `game/.godot/**`

## Paths To Preserve As Source

- `game/project.godot`
- `game/scenes/**`
- `game/scripts/**`
- `game/icon.svg`
- `game/icon.svg.import`
- `docs/**`
- `tools/engine-selftest/**`
- `build-workflows/**`
- `restaurant_simulator/**`

## Files Created

- `build-workflows/task-briefs/RS-GC-001.md`
- `build-workflows/audits/RS-GC-001-audit.md`
- `build-workflows/handoffs/RS-GC-001-handoff.md`
- `build-workflows/episodes/RS-GC-001/task.md`
- `build-workflows/episodes/RS-GC-001/trace.jsonl`
- `build-workflows/episodes/RS-GC-001/checks.jsonl`
- `build-workflows/episodes/RS-GC-001/audit.md`
- `build-workflows/episodes/RS-GC-001/handoff.md`
- `build-workflows/episodes/RS-GC-001/receipt.json`

## Files Modified

- `build-workflows/task-briefs/current-task.md`
- `build-workflows/state/state.json`

## Files Deleted

None.

## Required Local Verification

- Confirm `.gitignore` is present.
- Adjust local tracking status for covered generated artifact groups.
- Confirm generated files remain locally.
- Run Python tests.
- Run C# self-test.
- Run Godot editor smoke test.

## Status

Ready for review. Actual local tracking-status adjustment remains pending.
