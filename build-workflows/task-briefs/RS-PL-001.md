# Task Brief — RS-PL-001

## Task Metadata

Task ID: RS-PL-001  
Task Name: Plausibility and Realism Validation Profile  
Task Type: Validation Profile / Regression Tests  
Target Layer: Plausibility gates for synthetic simulator output  
Runtime Class: T3  
Owner: Michael Robertucci  
Approval Required: User requested RS-PL-001.  
Expected Output: Reusable plausibility profile, regression tests, audit, handoff, and receipt.  
Repository Branch: main

## Files Allowed To Modify

- build-workflows/state/state.json
- build-workflows/task-briefs/current-task.md
- build-workflows/task-briefs/RS-PL-001.md
- profiles/plausibility.json
- tests/test_plausibility.py
- build-workflows/audits/RS-PL-001-audit.md
- build-workflows/handoffs/RS-PL-001-handoff.md
- build-workflows/episodes/RS-PL-001/**

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

Create a reusable plausibility profile and test suite that checks whether synthetic simulator output is operationally coherent without claiming it is real operational evidence.

## Source Packets To Load

- 00 Security
- 01 System
- 02 Workflow
- 03 Schema
- 04 Context
- 05 Diagnostics
- build-workflows/episodes/RS-VA-001/receipt.json

## Existing Implementation Sources

- build-workflows/task-briefs/current-task.md
- build-workflows/state/state.json
- restaurant_simulator/engine.py
- restaurant_simulator/core.py
- tests/test_engine_contract.py
- tests/test_validation_gate.py

## Requirements

1. Activate RS-PL-001 in workflow state.
2. Add a reusable plausibility profile file.
3. Add tests for required realism categories from diagnostics doctrine.
4. Verify daypart demand shape is distinct and operationally coherent.
5. Verify scenario modifiers have directional effects.
6. Verify item lifecycle, prep, waste, call-off, equipment, local-event, overload, and recovery evidence exists where applicable.
7. Do not modify simulator runtime behavior.
8. Produce task audit, handoff, episode trace, checks, and receipt.

## Non-Goals

- No engine runtime changes.
- No source-pack doctrine edits.
- No committed generated output bundles.
- No live restaurant data.
- No ASC runtime integration.
- No customer-visible performance claims.

## Data Contracts

- The profile is a synthetic plausibility gate.
- Passing the profile means output is coherent enough for simulator validation, not proof of real-world accuracy.
- Failed profile checks must be handled by a follow-up task scoped to the relevant layer.

## Required Tests

- `python -m unittest discover -s tests`
- New plausibility tests in `tests/test_plausibility.py`

## Acceptance Criteria

1. Plausibility profile exists and declares all required realism categories.
2. Tests cover daypart, channel, menu, weather, traffic, event, lifecycle, prep, waste, staffing, equipment, overload, recovery, and multi-rush gates.
3. Runtime files remain unchanged.
4. Workflow receipt records review status.

## Stop Rule

If plausibility tests expose an engine behavior defect, do not change engine code in this task. Record the failure and create a follow-up engine or scenario-model task.
