# Task Brief — RS-AU-001

## Task Metadata

Task ID: RS-AU-001  
Task Name: Post-change Repository Stabilization Audit  
Task Type: Audit / Stabilization / Repository Hygiene  
Target Layer: Workflow state, Godot/game source, generated artifacts, validation evidence  
Runtime Class: T3  
Owner: Michael Robertucci  
Approval Required: User requested RS-AU-001 and explicitly instructed not to delete generated Godot/cache files before ignore rules are in place.  
Expected Output: Repository state audit, risk classification, cleanup plan, workflow reconciliation, receipt.  
Repository Branch: main

## Files Allowed To Modify

- .gitignore
- build-workflows/state/state.json
- build-workflows/task-briefs/current-task.md
- build-workflows/task-briefs/RS-AU-001.md
- build-workflows/audits/RS-AU-001-audit.md
- build-workflows/handoffs/RS-AU-001-handoff.md
- build-workflows/episodes/RS-AU-001/**

## Files Prohibited From Modifying

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

Audit all major repository changes after the last validated checkpoint, stabilize workflow truth, and prepare a safe cleanup path without deleting generated Godot/cache files.

## Source Packets To Load

- 00 Security
- 01 System
- 02 Workflow
- 03 Schema
- 04 Context
- 05 Diagnostics
- build-workflows/episodes/RS-VA-001/receipt.json
- build-workflows/episodes/RS-3D-001/receipt.json
- build-workflows/episodes/RS-ST-001/receipt.json

## Existing Implementation Sources

- .gitignore
- build-workflows/task-briefs/current-task.md
- build-workflows/state/state.json
- build-workflows/state.json
- game/project.godot
- game/scripts/sim/SimRunState.cs
- game/scripts/sim/SelfTest.cs
- tools/engine-selftest/Program.cs

## Requirements

1. Compare current `main` against the last known validated checkpoint.
2. Classify changed files into source, generated/cache, workflow, docs, and tooling.
3. Verify `.gitignore` protects generated Godot/cache paths before any cleanup.
4. Do not delete generated Godot/cache files in this task.
5. Identify workflow-state conflicts and authoritative state paths.
6. Identify contract/security risks.
7. Produce next best move.
8. Produce audit, handoff, episode trace/checks, and receipt.

## Non-Goals

- No generated/cache deletion.
- No runtime code changes.
- No Godot scene or script edits.
- No Python engine edits.
- No source-pack doctrine edits.
- No ASC integration.
- No public/customer-visible claims.

## Acceptance Criteria

1. `.gitignore` includes Godot/cache protection before tracked cleanup.
2. Audit classifies the major change groups.
3. Audit records duplicate or conflicting workflow-state paths.
4. Audit records generated/cache files still tracked and defers removal to a later cleanup task.
5. Audit records that no generated files were deleted in RS-AU-001.
6. Receipt states next recommended task.

## Stop Rule

If runtime defects or contract failures are suspected, do not patch runtime files in RS-AU-001. Record the issue and defer fixes to a scoped follow-up task.
