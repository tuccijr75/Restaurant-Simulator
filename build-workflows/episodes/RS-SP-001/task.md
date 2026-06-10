# Episode Task — RS-SP-001

## Task ID

RS-SP-001

## Task Name

Source Pack Workflow Activation

## Runtime Class

T2

## Lane

source

## Objective

Create the Restaurant Simulator control-pack and build-workflow bootstrap artifacts in the repository.

## Allowed Paths

- control-pack/manifest.json
- control-pack/active/**
- build-workflows/state/state.json
- build-workflows/task-briefs/current-task.md
- build-workflows/task-briefs/RS-SP-001.md
- build-workflows/audits/RS-SP-001-audit.md
- build-workflows/handoffs/RS-SP-001-handoff.md
- build-workflows/episodes/RS-SP-001/**

## Prohibited Paths

- restaurant_simulator/**
- game/**
- tests/**
- outputs/**
- README.md
- pyproject.toml
- .github/**
- AI Shift Commander repository or files

## Completion Criteria

- Source pack exists in `control-pack/active/`.
- Manifest exists.
- Current task points to RS-SP-001.
- Workflow state exists.
- Task brief, audit, handoff, episode trace/checks/audit/handoff/receipt exist.
- No prohibited paths were changed.
