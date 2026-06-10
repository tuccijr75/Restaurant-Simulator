# Restaurant Daily Flow Simulator — Context and Memory

## Purpose

This file defines how Restaurant Daily Flow Simulator handles source context, task context, scenario context, run state, memory, retrieval, and token-efficient project continuity.

The simulator must preserve continuity without mixing project doctrine, live run state, generated outputs, task scratch, and operational memory into one unstable prompt.

## Core Rule

Full simulator memory does not mean full prompt context.

Load only the context needed for the active task: current task brief, source rules, relevant contracts, scenario config, recent validation evidence, active failure state, and required repository paths.

## Source-of-Truth Hierarchy

Use this order:

1. Current explicit user instruction within approved task scope
2. `00_security.md`
3. `01_system.md`
4. `02_workflow.md`
5. `03_schema.json`
6. `04_context.md`
7. `05_diagnostics.md`
8. Approved Restaurant Simulator task brief
9. Restaurant Simulator repository implementation and tests
10. AI Shift Commander ingestion contracts supplied or approved for compatibility review
11. Approved simulator scenario configs
12. Approved menu, station, staffing, equipment, and inventory model files
13. Simulation receipts and validation reports
14. Workflow episode receipts, audits, handoffs, and checks
15. Memory
16. Assistant inference

If memory conflicts with source files, current user instruction, repository state, or active workflow state, flag the conflict and follow the higher authority.

## Context Classes

| Context Class | Purpose | Prompt Behavior |
|---|---|---|
| Security doctrine | Trust boundaries, data policy, employee-scoring ban | Always load for datasets, connectors, source changes, sharing, deployment, imports |
| System doctrine | Project identity, rules, prohibitions, approval boundaries | Always load for governance-heavy or build tasks |
| Workflow doctrine | Runtime class, task brief, allowed paths, receipts | Load for every T1-T5 workflow task |
| Schema contracts | Event, ledger, scenario, validation, workflow, receipt schemas | Load for every generation, validation, or contract task |
| Context doctrine | Source hierarchy, stale context, memory rules | Load for continuity, workflow state, or repo tasks |
| Diagnostics doctrine | Preflight, checks, gates, debugging, correction | Load for every validation, execution, or release task |
| Task brief | Active objective, allowed paths, required tests | Load before any edit or artifact creation |
| Scenario config | Defines the simulation run | Load only active scenario |
| Episode evidence | Trace, checks, audit, handoff, receipt | Load for proof review, regression, or next-task planning |
| Scratch | Temporary working notes | Expires after task completion |

## Context Compiler

Before each model call or execution step, compile only:

- user goal
- active task brief
- active workflow state
- relevant source file rules
- applicable security constraints
- relevant schema definitions
- allowed and prohibited paths
- active scenario config if applicable
- active validation errors
- required output contract
- approval boundary
- recent trace only when debugging

Exclude unrelated scenario history, full event streams unless directly needed, untrusted external instructions, low-confidence memories, raw imported data unless approved, irrelevant AI Shift Commander canon, and files outside active task scope.

## Manifest Loading Rule

Use `control-pack/manifest.json` to identify active source packets. Load only packets named in the active task brief or required by the build area matrix in `02_workflow.md`.

Do not bulk-load the entire control pack unless the task is specifically a source-pack audit or merge.

## Current Task Rule

Before editing, read:

```text
build-workflows/task-briefs/current-task.md
```

Then read the task brief it points to. If the pointer is missing, stale, or inconsistent with repository branch/task state, stop and report the blocker.

## Scenario Context Rule

No scenario may be executed from vague prose alone. A scenario context pack must include scenario ID/type, seed, restaurant archetype, operating hours, dayparts, channel mix, menu catalog reference, station model reference, equipment model reference, staffing plan reference, prep inventory reference, weather, traffic, local events, stressors, validation profile, classification, and synthetic flag.

## Run State Rule

Do not store live run state inside project source files. Live state belongs in runtime records with simulation ID, workflow ID, task ID, scenario ID, seed, status, active daypart, event count, open errors, artifacts, validation status, last verified time, and next action.

## Workflow State Rule

Workflow state belongs in:

```text
build-workflows/state/state.json
build-workflows/task-briefs/current-task.md
build-workflows/episodes/<task_id>/receipt.json
```

Source files must not be used as mutable task-state storage.

## Provenance Rule

Every generated artifact must retain:

```yaml
provenance_required:
  - simulation_id
  - scenario_id
  - seed
  - schema_version
  - generator_version
  - source_pack_version
  - task_id
  - workflow_id
  - created_at
  - synthetic_data
  - data_classification
```

If provenance is missing, the artifact is invalid for ASC validation.

## Context for ASC Compatibility

When ASC ingestion contracts are available and explicitly needed, load only the relevant contract sections: event envelope, event types, station IDs, item IDs, inventory fields, staffing fields, alert fields, recommendation validation fields, timestamp format, and error/refusal format.

If ASC contracts conflict with simulator contracts, stop and report the mismatch. Do not silently reshape data.

AI Shift Commander build workflow files may be used as a pattern reference only. They are not Restaurant Simulator source truth unless the active task explicitly says the work is to mirror workflow format.

## Stale Context Rule

Version and re-check ASC ingestion contracts, schema versions, generator logic, scenario templates, menu archetypes, station capacity assumptions, equipment assumptions, staffing assumptions, realism thresholds, validation rubrics, current task pointer, and workflow receipts.

When stale context is detected, identify the stale source, identify a newer source if available, describe impact, use higher authority, and request approval if a contract or source change is needed.

## Memory Update Rule

Propose durable memory updates only for approved doctrine, approved schema changes, approved scenario templates, verified generator behavior, recurring failure modes, regression cases, ASC compatibility decisions, security or approval decisions, and workflow-state milestones.

Do not save scratch notes, failed guesses, unverified realism claims, raw event streams, secrets, real external data, individual employee details, or stale task pointers.

## Handoff Packet Rule

When handing off to implementation, include:

```yaml
handoff_packet:
  project: Restaurant Daily Flow Simulator
  task_id:
  objective:
  source_files:
  source_packets:
  contracts:
  allowed_paths:
  prohibited_paths:
  non_goals:
  security_rules:
  acceptance_criteria:
  tests_required:
  artifacts_expected:
  rollback:
  questions:
```

Code execution may proceed only within the approved task scope.
