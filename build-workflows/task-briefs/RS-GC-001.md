# Task Brief — RS-GC-001

## Task Metadata

Task ID: RS-GC-001  
Task Name: Repository Tracking Hygiene Plan  
Task Type: Repository Hygiene / Local-First Tracking Plan  
Target Layer: Git tracking status for generated Godot and build artifacts  
Runtime Class: T3  
Owner: Michael Robertucci  
Approval Required: User said to continue after expanding `.gitignore`.  
Expected Output: Safe local tracking plan, artifact groups, workflow receipt.  
Repository Branch: main

## Safety Rule

Generated Godot/cache/build files must stay on the user's local disk. This task records the safe plan only. Tracking-status changes should be performed from the user's local clone.

## Files Allowed To Modify

- build-workflows/state/state.json
- build-workflows/task-briefs/current-task.md
- build-workflows/task-briefs/RS-GC-001.md
- build-workflows/audits/RS-GC-001-audit.md
- build-workflows/handoffs/RS-GC-001-handoff.md
- build-workflows/episodes/RS-GC-001/**

## Files Prohibited From Modifying In This Task

- restaurant_simulator/**
- game/**
- tools/**
- docs/**
- outputs/**
- control-pack/active/**
- README.md
- pyproject.toml
- AI Shift Commander repository or files

## Objective

Record the safe repository tracking plan after `.gitignore` expansion. The plan identifies generated artifact groups that should no longer be tracked in a later local action while preserving local files.

## Observed Generated Artifact Groups

- `game/.godot/editor/**`
- `game/.godot/mono/temp/**`
- `game/.godot/shader_cache/**`
- `game/.godot/uid_cache.bin`
- compiled artifacts under `game/.godot/mono/temp/**`
- cache artifacts under `game/.godot/**`

## Keep In Repository

- `game/project.godot`
- `game/scenes/**`
- `game/scripts/**`
- `game/icon.svg`
- `game/icon.svg.import`
- `docs/**`
- `tools/engine-selftest/**` source files
- `build-workflows/**` source-pack and workflow evidence
- `restaurant_simulator/**`

## Required Local Procedure

1. Confirm `.gitignore` is committed.
2. Adjust tracking status locally for generated artifact groups covered by `.gitignore`.
3. Confirm local generated files still exist on disk.
4. Run test/build checks.
5. Commit only the tracking-status changes.

## Required Verification

- Python tests: `python -m unittest discover -s tests`
- C# self-test: run `tools/engine-selftest` from the local environment
- Godot editor smoke test: open `game/` and run the main scene
- Confirm source files under `game/scripts`, `game/scenes`, `restaurant_simulator`, `tools/engine-selftest`, and `docs` are preserved

## Stop Rule

If any source path appears in the pending tracking-status changes, stop and restore the source path before committing.
