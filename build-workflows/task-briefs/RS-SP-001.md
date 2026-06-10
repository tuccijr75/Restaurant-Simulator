# Task Brief — RS-SP-001

## Task Metadata

Task ID: RS-SP-001  
Task Name: Source Pack Workflow Activation  
Task Type: Source Pack / Workflow Bootstrap  
Target Layer: control-pack and build-workflows  
Runtime Class: T2  
Owner: Michael Robertucci  
Approval Required: User explicitly requested adding the rewritten source files and creating the initial workflow task.  
Expected Output: Active Restaurant Simulator control pack, workflow state, current task pointer, task brief, audit, handoff, episode package, and receipt.  
Repository Branch: main

Files Allowed To Modify:
- control-pack/manifest.json
- control-pack/active/00_security/00_security.md
- control-pack/active/01_system/01_system.md
- control-pack/active/02_workflow/02_workflow.md
- control-pack/active/03_schema/03_schema.json
- control-pack/active/04_context/04_context.md
- control-pack/active/05_diagnostics/05_diagnostics.md
- build-workflows/state/state.json
- build-workflows/task-briefs/current-task.md
- build-workflows/task-briefs/RS-SP-001.md
- build-workflows/audits/RS-SP-001-audit.md
- build-workflows/handoffs/RS-SP-001-handoff.md
- build-workflows/episodes/RS-SP-001/task.md
- build-workflows/episodes/RS-SP-001/trace.jsonl
- build-workflows/episodes/RS-SP-001/checks.jsonl
- build-workflows/episodes/RS-SP-001/audit.md
- build-workflows/episodes/RS-SP-001/handoff.md
- build-workflows/episodes/RS-SP-001/receipt.json

Files Prohibited From Modifying:
- restaurant_simulator/**
- game/**
- tests/**
- outputs/**
- README.md
- pyproject.toml
- .github/**
- AI Shift Commander repository or files

## Task Objective

Activate the Restaurant Simulator workflow source pack using the AI Shift Commander-style build discipline while preserving Restaurant Simulator-specific doctrine, contracts, standalone operation, synthetic-data-only default, and split item lifecycle semantics.

## Source Packets To Load

Required baseline:
- 00 Security
- 01 System
- 02 Workflow
- 03 Schema
- 04 Context
- 05 Diagnostics

Task-specific packets:
- None beyond the initial source-pack files being activated.

## Existing Implementation Sources

Inspect before editing:
- README.md
- restaurant_simulator/engine.py
- game/scripts/sim/SimRunState.cs
- control-pack/manifest.json, if present
- build-workflows/task-briefs/current-task.md, if present

## Source-of-Truth Rules

Use Restaurant Simulator repository source pack as source of truth after activation. Do not modify AI Shift Commander. Do not use stale `item.sold` doctrine. Do not silently edit implementation files. If required source is missing, record the missing source and continue only if this task is explicitly bootstrapping that source.

## Requirements

1. Add active source packets under `control-pack/active/`.
2. Add `control-pack/manifest.json`.
3. Add `build-workflows/state/state.json`.
4. Add `build-workflows/task-briefs/current-task.md`.
5. Add this initial task brief.
6. Add audit, handoff, trace, checks, and receipt artifacts.
7. Preserve standalone simulator boundary.
8. Preserve synthetic-data-only default.
9. Preserve no employee scoring.
10. Preserve `item.taken -> item.completed` lifecycle doctrine.
11. Do not modify simulator implementation code in this task.

## Non-Goals

- No Python engine changes.
- No Godot implementation changes.
- No tests modified.
- No README changes.
- No ASC repository changes.
- No live ASC connection.
- No real restaurant data import.
- No schema migration beyond source-pack activation.

## Data Contracts

This task activates source-pack and workflow contracts only:
- control-pack manifest contract
- task brief contract
- workflow state contract
- episode package contract
- receipt contract
- event doctrine contract: `item.taken -> item.completed`

## Approval Boundaries

Stop for approval before:
- source-pack doctrine changes beyond the provided rewritten pack
- security changes beyond the provided rewritten pack
- production deployment
- new integration
- ASC runtime connection
- real data import
- simulator implementation changes

## Implementation Constraints

Preserve standalone simulator behavior. Preserve synthetic-data-only default. Preserve no employee scoring. Preserve deterministic replay doctrine. Preserve causal, non-random-only simulation. Preserve item lifecycle semantics: `item.taken -> item.completed`. Preserve output provenance. Preserve receipt/audit/handoff requirements.

## Required Tests

- Confirm previously absent workflow paths are created.
- Confirm files remain within allowed paths.
- Confirm no prohibited implementation files are changed.
- Confirm manifest lists all six active source packets.
- Confirm current task pointer references RS-SP-001.
- Local unit tests are not run because this task does not modify runtime implementation.

## Acceptance Criteria

1. `control-pack/manifest.json` exists.
2. Six active source packets exist.
3. `build-workflows/state/state.json` exists.
4. `build-workflows/task-briefs/current-task.md` points to RS-SP-001.
5. RS-SP-001 task brief exists.
6. RS-SP-001 audit, handoff, episode trace/checks/audit/handoff/receipt exist.
7. No implementation files are modified.
8. Next task is identified as RS-SC-001.

## Audit Checklist

- Source-pack compliance checked.
- Required packets loaded.
- Deprecated source scan completed at doctrine level.
- No unrelated files changed.
- Tests added or updated only when needed.
- Security impact checked.
- Determinism checked when applicable.
- Ledger reconciliation checked when applicable.
- Failure/degraded mode checked.
- Handoff produced.
- Receipt produced.

## Required Handoff Output

The builder must return:
- files changed
- files created
- source packets used
- tests added
- tests run
- source conflicts found
- assumptions introduced
- blockers
- next recommended task
- approval required status

## Stop Rule

If any required packet, schema, contract, decision, or implementation source is missing outside the explicit bootstrap scope, stop and report the missing dependency instead of inventing content.
