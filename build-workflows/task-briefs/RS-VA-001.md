# Task Brief — RS-VA-001

## Task Metadata

Task ID: RS-VA-001  
Task Name: Determinism and Output Gate Verification  
Task Type: Validation / Determinism / Output Gate  
Target Layer: Python simulator outputs and validation tests  
Runtime Class: T3  
Owner: Michael Robertucci  
Approval Required: User requested RS-VA-001.  
Expected Output: Determinism and output-gate tests plus validation receipt, audit, and handoff.  
Repository Branch: main

## Files Allowed To Modify

- build-workflows/state/state.json
- build-workflows/task-briefs/current-task.md
- build-workflows/task-briefs/RS-VA-001.md
- tests/test_validation_gate.py
- build-workflows/audits/RS-VA-001-audit.md
- build-workflows/handoffs/RS-VA-001-handoff.md
- build-workflows/episodes/RS-VA-001/**

## Files Prohibited From Modifying

- restaurant_simulator/engine.py
- restaurant_simulator/core.py
- game/**
- outputs/**
- README.md
- pyproject.toml
- .github/**
- control-pack/active/**
- AI Shift Commander repository or files

## Objective

Verify seeded deterministic replay and output bundle completeness across scenario types without changing simulator runtime behavior.

## Source Packets To Load

- 00 Security
- 01 System
- 02 Workflow
- 03 Schema
- 04 Context
- 05 Diagnostics
- build-workflows/episodes/RS-EN-001/receipt.json

## Existing Implementation Sources

- build-workflows/task-briefs/current-task.md
- build-workflows/state/state.json
- restaurant_simulator/engine.py
- restaurant_simulator/core.py
- tests/test_engine_contract.py

## Requirements

1. Activate RS-VA-001 in workflow state.
2. Add deterministic replay and output-gate tests without changing runtime logic.
3. Verify every configured scenario is deterministic for fixed seeds.
4. Verify `run_to_path` writes the complete output bundle.
5. Verify output file digests match the run receipt where recorded.
6. Verify generated outputs remain synthetic and contain no prohibited simulator-governance markers.
7. Verify deprecated `item.sold` is absent from generated event streams.
8. Produce audit, handoff, episode trace, checks, and receipt.

## Non-Goals

- No engine behavior changes.
- No source-pack doctrine edits.
- No committed generated output artifacts.
- No ASC runtime integration.
- No live data usage.

## Data Contracts

- Same seed and scenario must produce identical object outputs.
- `run_to_path` must produce event stream, inventory ledger, staffing ledger, recommendation validation dataset, alert validation dataset, end-of-shift summary, run receipt, and hashes file.
- Receipt must report deterministic replay success.
- Output files must be parseable JSON or JSONL.

## Required Tests

- `python -m unittest discover -s tests`
- New validation gate tests in `tests/test_validation_gate.py`

## Acceptance Criteria

1. All local unit tests pass.
2. Deterministic replay passes across every scenario type.
3. Output bundle completeness passes.
4. Output digest verification passes for files recorded in the receipt.
5. Governance scan passes.
6. Workflow receipt records completed status.

## Stop Rule

If validation identifies engine defects, do not modify engine in this task. Record failure details and defer corrective runtime work to a new engine task.
