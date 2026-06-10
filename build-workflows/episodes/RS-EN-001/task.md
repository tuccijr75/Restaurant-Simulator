# Episode Task — RS-EN-001

## Task ID

RS-EN-001

## Task Name

Engine Lifecycle and Provenance Verification

## Runtime Class

T3

## Lane

engine

## Objective

Verify and correct engine lifecycle/provenance behavior against the active source-pack rules.

## Allowed Paths

- build-workflows/state/state.json
- build-workflows/task-briefs/current-task.md
- build-workflows/task-briefs/RS-EN-001.md
- restaurant_simulator/engine.py
- tests/test_engine_contract.py
- build-workflows/audits/RS-EN-001-audit.md
- build-workflows/handoffs/RS-EN-001-handoff.md
- build-workflows/episodes/RS-EN-001/**

## Prohibited Paths

- game/**
- outputs/**
- README.md
- pyproject.toml
- .github/**
- control-pack/active/**
- AI Shift Commander repository or files

## Completion Criteria

- Engine carries required provenance metadata.
- Item lifecycle remains split into item.taken and item.completed.
- Deprecated item.sold remains absent.
- Tests cover provenance and item instance traceability.
- Workflow artifacts and receipt exist.
