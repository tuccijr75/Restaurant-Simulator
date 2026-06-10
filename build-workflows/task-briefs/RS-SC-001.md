# Task Brief — RS-SC-001

## Task Metadata

Task ID: RS-SC-001  
Task Name: Schema and Contract Regression Alignment  
Task Type: Schema / Contract / Regression Tests  
Target Layer: control-pack schema and Python contract tests  
Runtime Class: T2  
Owner: Michael Robertucci  
Approval Required: User requested RS-SC-001.  
Expected Output: Source-pack-aware contract regression tests proving the active event lifecycle uses `item.taken -> item.completed` and rejects deprecated `item.sold`.  
Repository Branch: main

Files Allowed To Modify:
- build-workflows/state/state.json
- build-workflows/task-briefs/current-task.md
- build-workflows/task-briefs/RS-SC-001.md
- tests/test_engine_contract.py
- build-workflows/audits/RS-SC-001-audit.md
- build-workflows/handoffs/RS-SC-001-handoff.md
- build-workflows/episodes/RS-SC-001/task.md
- build-workflows/episodes/RS-SC-001/trace.jsonl
- build-workflows/episodes/RS-SC-001/checks.jsonl
- build-workflows/episodes/RS-SC-001/audit.md
- build-workflows/episodes/RS-SC-001/handoff.md
- build-workflows/episodes/RS-SC-001/receipt.json

Files Prohibited From Modifying:
- restaurant_simulator/**
- game/**
- outputs/**
- README.md
- pyproject.toml
- .github/**
- control-pack/active/**
- AI Shift Commander repository or files

## Task Objective

Align the active test suite with the Restaurant Simulator source-pack workflow and split item lifecycle contract by adding regressions that validate `item.taken`, `item.completed`, ticket completion ordering, deprecated `item.sold` absence, and source-pack schema event enum agreement.

## Source Packets To Load

Required baseline:
- 00 Security
- 01 System
- 02 Workflow
- 03 Schema
- 04 Context
- 05 Diagnostics

Task-specific packets:
- None beyond active source-pack files.

## Existing Implementation Sources

Inspect before editing:
- build-workflows/task-briefs/current-task.md
- build-workflows/state/state.json
- control-pack/active/01_system/01_system.md
- control-pack/active/03_schema/03_schema.json
- restaurant_simulator/engine.py
- restaurant_simulator/core.py
- game/scripts/sim/SimRunState.cs
- tests/test_engine_contract.py

## Source-of-Truth Rules

Use Restaurant Simulator source pack as source of truth. Do not use deprecated `item.sold` doctrine. Do not modify runtime implementation in this task. Do not modify AI Shift Commander. If tests reveal runtime mismatch, record it as a blocker for RS-EN-001 or RS-VA-001 rather than silently expanding scope.

## Requirements

1. Update `current-task.md` to activate RS-SC-001.
2. Update `state.json` to mark RS-SC-001 active or ready for review after completion.
3. Add regression coverage proving generated events do not contain `item.sold`.
4. Add regression coverage proving every item completion is preceded by a matching item taken event.
5. Add regression coverage proving completed tickets occur only after all known items for the order are completed.
6. Add regression coverage proving active schema event enum matches exported Python `EVENT_TYPES`.
7. Preserve no employee scoring and synthetic-data-only expectations.
8. Produce audit, handoff, episode package, and receipt.

## Non-Goals

- No Python runtime implementation changes.
- No Godot runtime implementation changes.
- No control-pack doctrine changes.
- No generated output refresh.
- No live ASC compatibility run.
- No AI Shift Commander changes.

## Data Contracts

- Event type enum: `order.created`, `item.taken`, `item.completed`, `ticket.updated`, `staff.assignment.updated`, `prep.confirmed`, `waste.recorded`, `station.overloaded`, `station.recovered`, `shift.started`, `shift.ended`.
- Deprecated event type: `item.sold` must be absent from active generated streams.
- Lifecycle invariant: `item.taken -> item.completed -> ticket.updated completed`.

## Approval Boundaries

Stop for approval before:
- source-pack doctrine change
- schema migration changing contract semantics
- runtime implementation change
- ASC runtime connection
- real data import
- production deployment

## Implementation Constraints

Preserve standalone simulator behavior. Preserve synthetic-data-only default. Preserve no employee scoring. Preserve deterministic replay doctrine. Preserve item lifecycle semantics: `item.taken -> item.completed`. Preserve output provenance. Preserve receipt/audit/handoff requirements.

## Required Tests

- `python -m unittest discover -s tests` should be run in a local or CI workspace after this commit.
- Connector environment records test changes but does not execute local Python runtime.

## Acceptance Criteria

1. `tests/test_engine_contract.py` checks source-pack schema enum against exported `EVENT_TYPES`.
2. `tests/test_engine_contract.py` rejects `item.sold` in generated streams.
3. `tests/test_engine_contract.py` validates `item.taken -> item.completed` ordering.
4. `tests/test_engine_contract.py` validates ticket completion after item completion.
5. Current task and workflow state reflect RS-SC-001.
6. Audit, handoff, episode trace, checks, audit, handoff, and receipt exist.

## Audit Checklist

- Source-pack compliance checked.
- Required packets loaded.
- Deprecated source scan completed.
- No unrelated files changed.
- Tests added or updated.
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

If any required packet, schema, contract, decision, or implementation source is missing, stop and report the missing dependency instead of inventing content.
