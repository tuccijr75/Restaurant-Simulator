# Episode Task — RS-VA-001

## Task ID

RS-VA-001

## Task Name

Determinism and Output Gate Verification

## Runtime Class

T3

## Lane

validation

## Objective

Verify deterministic replay and output bundle gates without changing simulator runtime behavior.

## Allowed Paths

- build-workflows/state/state.json
- build-workflows/task-briefs/current-task.md
- build-workflows/task-briefs/RS-VA-001.md
- tests/test_validation_gate.py
- build-workflows/audits/RS-VA-001-audit.md
- build-workflows/handoffs/RS-VA-001-handoff.md
- build-workflows/episodes/RS-VA-001/**

## Prohibited Paths

- restaurant_simulator/engine.py
- restaurant_simulator/core.py
- game/**
- outputs/**
- README.md
- pyproject.toml
- .github/**
- control-pack/active/**
- AI Shift Commander repository or files

## Completion Criteria

- Validation gate tests exist.
- No runtime code changed.
- Output bundle and digest gates are covered.
- Local/CI test run result is recorded.
- Workflow artifacts and receipt exist.
