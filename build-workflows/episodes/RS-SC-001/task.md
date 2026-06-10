# Episode Task — RS-SC-001

## Task ID

RS-SC-001

## Task Name

Schema and Contract Regression Alignment

## Runtime Class

T2

## Lane

schema

## Objective

Align contract tests with the active Restaurant Simulator source pack and item lifecycle contract.

## Allowed Paths

- build-workflows/state/state.json
- build-workflows/task-briefs/current-task.md
- build-workflows/task-briefs/RS-SC-001.md
- tests/test_engine_contract.py
- build-workflows/audits/RS-SC-001-audit.md
- build-workflows/handoffs/RS-SC-001-handoff.md
- build-workflows/episodes/RS-SC-001/**

## Prohibited Paths

- restaurant_simulator/**
- game/**
- outputs/**
- README.md
- pyproject.toml
- .github/**
- control-pack/active/**
- AI Shift Commander repository or files

## Completion Criteria

- Contract tests reject `item.sold`.
- Contract tests prove `item.taken -> item.completed`.
- Contract tests prove ticket completion after item completion.
- Contract tests verify schema enum parity with exported `EVENT_TYPES`.
- Workflow artifacts and receipt exist.
