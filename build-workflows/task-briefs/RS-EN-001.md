# Task Brief — RS-EN-001

## Task Metadata

Task ID: RS-EN-001  
Task Name: Engine Lifecycle and Provenance Verification  
Task Type: Engine / Provenance / Contract Verification  
Target Layer: Python simulation engine and contract tests  
Runtime Class: T3  
Owner: Michael Robertucci  
Approval Required: User requested RS-EN-001.  
Expected Output: Verified and, if needed, corrected engine lifecycle/provenance behavior with tests and workflow receipt.  
Repository Branch: main

Files Allowed To Modify:
- build-workflows/state/state.json
- build-workflows/task-briefs/current-task.md
- build-workflows/task-briefs/RS-EN-001.md
- restaurant_simulator/engine.py
- tests/test_engine_contract.py
- build-workflows/audits/RS-EN-001-audit.md
- build-workflows/handoffs/RS-EN-001-handoff.md
- build-workflows/episodes/RS-EN-001/task.md
- build-workflows/episodes/RS-EN-001/trace.jsonl
- build-workflows/episodes/RS-EN-001/checks.jsonl
- build-workflows/episodes/RS-EN-001/audit.md
- build-workflows/episodes/RS-EN-001/handoff.md
- build-workflows/episodes/RS-EN-001/receipt.json

Files Prohibited From Modifying:
- game/**
- outputs/**
- README.md
- pyproject.toml
- .github/**
- control-pack/active/**
- AI Shift Commander repository or files

## Task Objective

Verify the Python engine against the active source-pack lifecycle and provenance rules. Correct only engine/provenance gaps discovered during inspection, preserve deterministic behavior, and add tests proving output provenance and item lifecycle integrity.

## Source Packets To Load

Required baseline:
- 00 Security
- 01 System
- 02 Workflow
- 03 Schema
- 04 Context
- 05 Diagnostics

Task-specific packets:
- RS-SC-001 receipt and test changes.

## Existing Implementation Sources

Inspect before editing:
- build-workflows/task-briefs/current-task.md
- build-workflows/state/state.json
- build-workflows/episodes/RS-SC-001/receipt.json
- control-pack/active/04_context/04_context.md
- restaurant_simulator/engine.py
- restaurant_simulator/core.py
- tests/test_engine_contract.py

## Source-of-Truth Rules

Use the active Restaurant Simulator source pack. Do not modify AI Shift Commander. Do not change source-pack doctrine. Do not modify Godot in this task. If engine changes are required, keep them bounded to lifecycle/provenance only.

## Requirements

1. Activate RS-EN-001 in current task and workflow state.
2. Verify `item.taken -> item.completed -> ticket.updated completed` ordering.
3. Verify deprecated `item.sold` remains absent.
4. Verify generated artifacts carry required provenance where schema permits.
5. Add or update tests for provenance fields.
6. Preserve deterministic replay.
7. Preserve synthetic-data-only and no employee-scoring constraints.
8. Produce audit, handoff, episode package, and receipt.

## Non-Goals

- No Godot changes.
- No control-pack doctrine changes.
- No output regeneration committed to repo.
- No ASC runtime connection.
- No real data import.

## Data Contracts

- Lifecycle: `item.taken -> item.completed -> ticket.updated completed`.
- Deprecated event: `item.sold` must not appear in active streams.
- Provenance: generated ledgers, validation datasets, summaries, run receipts, and run metadata must include source-pack/version/task/workflow/security metadata.

## Approval Boundaries

Stop for approval before source-pack doctrine changes, schema-breaking migrations, ASC runtime connection, real data import, hosted deployment, public dataset release, or AI Shift Commander modification.

## Implementation Constraints

Preserve standalone simulator behavior. Preserve synthetic-data-only default. Preserve no employee scoring. Preserve deterministic replay. Preserve causal, non-random-only simulation. Preserve item lifecycle semantics. Do not weaken existing tests.

## Required Tests

- Add provenance tests to `tests/test_engine_contract.py`.
- Run locally or in CI: `python -m unittest discover -s tests`.

## Acceptance Criteria

1. Engine outputs include required provenance metadata where schema permits.
2. Item instance lifecycle is traceable from taken to completed.
3. Existing contract tests still apply.
4. Current task and workflow state mark RS-EN-001 ready for review after completion.
5. Audit, handoff, checks, trace, and receipt exist.

## Audit Checklist

- Source-pack compliance checked.
- Required packets loaded.
- Deprecated source scan completed.
- No unrelated files changed.
- Tests added or updated.
- Security impact checked.
- Determinism checked when applicable.
- Lifecycle checked.
- Provenance checked.
- Handoff produced.
- Receipt produced.

## Required Handoff Output

The builder must return files changed, files created, source packets used, tests added, tests run, conflicts, assumptions, blockers, next recommended task, and approval status.

## Stop Rule

If verification reveals issues outside engine lifecycle/provenance scope, record a blocker and defer to the correct task instead of expanding scope silently.
